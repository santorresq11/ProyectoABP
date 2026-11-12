"""
Entrena el modelo BASE (línea de comparación) de alerta temprana de deserción escolar y exporta
el artefacto que consume la API .NET (backend/src/AulaNube.Api/Modelo/modelo-riesgo.json).

- Modelo: regresión logística regularizada (L2) implementada con NumPy, entrenada por descenso de gradiente.
- Por qué existe: es interpretable (cada coeficiente es la contribución de una variable) y sirve como
  referencia mínima que la red LSTM (train_lstm.py) debe superar antes de pasar a producción.
- Explicabilidad: la API calcula la contribución de cada variable = coeficiente * valor estandarizado,
  equivalente a los valores SHAP de un modelo lineal.

Uso:
  python generar_datos_sinteticos.py && python entrenar_modelo_base.py
"""

from __future__ import annotations

import json
from datetime import date
from pathlib import Path

import numpy as np
import pandas as pd

RAIZ = Path(__file__).parent
DATOS = RAIZ / "data" / "estudiantes_tabular.csv"
SALIDA_API = RAIZ.parent / "backend" / "src" / "AulaNube.Api" / "Modelo" / "modelo-riesgo.json"
SALIDA_METRICAS = RAIZ / "artefactos" / "metricas_modelo_base.json"

VARIABLES = {
    "promedio_notas": "Promedio de notas del último periodo (1.0 a 5.0)",
    "tendencia_notas": "Cambio del promedio entre el primer y el último periodo",
    "asignaturas_perdidas": "Asignaturas con nota inferior a 3.0 en el último periodo",
    "asistencia": "Porcentaje de asistencia del último periodo",
    "tendencia_asistencia": "Cambio de la asistencia entre el primer y el último periodo",
    "extraedad": "Años por encima de la edad esperada para el grado",
    "repitente": "El estudiante repitió algún grado",
    "trabaja": "El estudiante trabaja fuera del horario escolar",
    "distancia_km": "Distancia entre la vivienda y la sede (km)",
    "estrato": "Estrato socioeconómico de la vivienda (DANE)",
    "beneficiario_pae": "Recibe el Programa de Alimentación Escolar",
}


def sigmoide(z: np.ndarray) -> np.ndarray:
    return 1.0 / (1.0 + np.exp(-z))


def auc_roc(y: np.ndarray, p: np.ndarray) -> float:
    orden = np.argsort(p)
    rangos = np.empty_like(orden, dtype=float)
    rangos[orden] = np.arange(1, len(p) + 1)
    pos = y == 1
    n_pos, n_neg = pos.sum(), (~pos).sum()
    return float((rangos[pos].sum() - n_pos * (n_pos + 1) / 2) / (n_pos * n_neg))


def metricas(y: np.ndarray, p: np.ndarray, umbral: float) -> dict[str, float]:
    pred = (p >= umbral).astype(int)
    tp = int(((pred == 1) & (y == 1)).sum())
    fp = int(((pred == 1) & (y == 0)).sum())
    fn = int(((pred == 0) & (y == 1)).sum())
    tn = int(((pred == 0) & (y == 0)).sum())
    precision = tp / (tp + fp) if tp + fp else 0.0
    recall = tp / (tp + fn) if tp + fn else 0.0
    f1 = 2 * precision * recall / (precision + recall) if precision + recall else 0.0
    return {
        "accuracy": round((tp + tn) / len(y), 4),
        "precision": round(precision, 4),
        "recall": round(recall, 4),
        "f1": round(f1, 4),
        "roc_auc": round(auc_roc(y, p), 4),
    }


def main() -> None:
    df = pd.read_csv(DATOS)
    rng = np.random.default_rng(7)
    idx = rng.permutation(len(df))
    corte = int(0.8 * len(df))
    entreno, prueba = df.iloc[idx[:corte]], df.iloc[idx[corte:]]

    columnas = list(VARIABLES)
    media = entreno[columnas].mean().to_numpy()
    desv = entreno[columnas].std().replace(0, 1).to_numpy()

    def estandarizar(d: pd.DataFrame) -> np.ndarray:
        return (d[columnas].to_numpy(dtype=float) - media) / desv

    x_tr, y_tr = estandarizar(entreno), entreno["deserta"].to_numpy()
    x_te, y_te = estandarizar(prueba), prueba["deserta"].to_numpy()

    # Ponderación de clases para compensar el desbalance (pocos casos de deserción)
    peso_pos = (y_tr == 0).sum() / (y_tr == 1).sum()
    pesos = np.where(y_tr == 1, peso_pos, 1.0)

    w, b, lr, l2 = np.zeros(len(columnas)), 0.0, 0.1, 0.01
    for _ in range(3000):
        p = sigmoide(x_tr @ w + b)
        error = (p - y_tr) * pesos
        w -= lr * (x_tr.T @ error / len(y_tr) + l2 * w)
        b -= lr * error.mean()

    # Calibración simple del intercepto para devolver probabilidades en la escala real
    # (la ponderación de clases infla las probabilidades).
    b_calibrado = b - np.log(peso_pos)

    p_te = sigmoide(x_te @ w + b_calibrado)
    umbral_medio, umbral_alto = 0.15, 0.40
    resultado = {
        "fecha": date.today().isoformat(),
        "n_entrenamiento": int(len(y_tr)),
        "n_prueba": int(len(y_te)),
        "tasa_desercion_prueba": round(float(y_te.mean()), 4),
        "umbral_alerta_media": metricas(y_te, p_te, umbral_medio),
        "umbral_alerta_alta": metricas(y_te, p_te, umbral_alto),
    }

    modelo = {
        "version": f"base-logistica-{date.today():%Y%m%d}",
        "descripcion": "Regresión logística L2 (línea base). Reemplazable por la LSTM exportada a ONNX.",
        "intercepto": round(float(b_calibrado), 6),
        "umbrales": {"medio": umbral_medio, "alto": umbral_alto},
        "variables": [
            {
                "nombre": nombre,
                "descripcion": VARIABLES[nombre],
                "media": round(float(media[i]), 6),
                "desviacion": round(float(desv[i]), 6),
                "peso": round(float(w[i]), 6),
            }
            for i, nombre in enumerate(columnas)
        ],
        "metricas": resultado,
    }

    SALIDA_API.parent.mkdir(parents=True, exist_ok=True)
    SALIDA_METRICAS.parent.mkdir(parents=True, exist_ok=True)
    SALIDA_API.write_text(json.dumps(modelo, ensure_ascii=False, indent=2), encoding="utf-8")
    SALIDA_METRICAS.write_text(json.dumps(resultado, ensure_ascii=False, indent=2), encoding="utf-8")

    print(json.dumps(resultado, ensure_ascii=False, indent=2))
    print("\nPesos (contribución por desviación estándar):")
    for nombre, peso in sorted(zip(columnas, w), key=lambda t: -abs(t[1])):
        print(f"  {nombre:22s} {peso:+.3f}")


if __name__ == "__main__":
    main()
