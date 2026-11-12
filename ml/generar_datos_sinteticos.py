"""
Genera un dataset SINTÉTICO de estudiantes de colegios públicos colombianos para el prototipo.

Por qué sintético: los datos reales de estudiantes son de menores de edad y están protegidos
(Ley 1581 de 2012). Para el prototipo se simulan distribuciones inspiradas en indicadores
públicos agregados (tasas de deserción intra-anual del MEN, estratos del DANE, etc.).
En producción el dataset vendría del SIMAT y del registro de notas/asistencia de la plataforma,
seudonimizado.

Salida:
  data/estudiantes_periodos.csv  -> una fila por estudiante y periodo (serie temporal, insumo LSTM)
  data/estudiantes_tabular.csv   -> una fila por estudiante con variables agregadas + etiqueta

Uso:
  python generar_datos_sinteticos.py --n 3000 --semilla 42
"""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
import pandas as pd

PERIODOS = 4
ASIGNATURAS = ["Matemáticas", "Lenguaje", "Ciencias", "Sociales", "Inglés"]


def generar(n: int, semilla: int) -> tuple[pd.DataFrame, pd.DataFrame]:
    rng = np.random.default_rng(semilla)

    grado = rng.integers(6, 12, n)  # secundaria y media (6° a 11°)
    extraedad = rng.choice([0, 0, 0, 0, 1, 1, 2, 3], n)
    edad = grado + 6 + extraedad  # se considera extraedad estar por encima de grado + 6 años
    estrato = rng.choice([1, 1, 1, 2, 2, 3], n)
    zona_rural = rng.random(n) < 0.35
    distancia = np.where(zona_rural, rng.gamma(3.0, 2.2, n), rng.gamma(1.5, 0.8, n)).round(1)
    trabaja = rng.random(n) < (0.08 + 0.12 * zona_rural + 0.04 * (grado >= 9))
    repitente = rng.random(n) < (0.06 + 0.10 * (extraedad > 0))
    pae = rng.random(n) < 0.7

    # Variable latente de "vulnerabilidad" que afecta notas y asistencia a lo largo del año
    vulnerabilidad = (
        0.6 * extraedad
        + 0.9 * trabaja
        + 0.5 * repitente
        + 0.08 * distancia
        - 0.25 * (estrato - 1)
        - 0.3 * pae
        + rng.normal(0, 0.8, n)
    )

    filas_periodo = []
    nota_base = 3.9 - 0.25 * vulnerabilidad + rng.normal(0, 0.3, n)
    asist_base = 0.95 - 0.03 * vulnerabilidad + rng.normal(0, 0.02, n)
    deterioro = np.clip(rng.normal(0.05 * vulnerabilidad, 0.08, n), -0.1, 0.4)

    promedios = np.zeros((n, PERIODOS))
    asistencias = np.zeros((n, PERIODOS))
    perdidas = np.zeros((n, PERIODOS), dtype=int)

    for p in range(PERIODOS):
        for i in range(n):
            notas = np.clip(nota_base[i] - deterioro[i] * p * 1.5 + rng.normal(0, 0.45, len(ASIGNATURAS)), 1.0, 5.0)
            asis = float(np.clip(asist_base[i] - deterioro[i] * p * 0.12 + rng.normal(0, 0.02), 0.3, 1.0))
            promedios[i, p] = notas.mean()
            asistencias[i, p] = asis
            perdidas[i, p] = int((notas < 3.0).sum())
            filas_periodo.append(
                {
                    "estudiante_id": i,
                    "periodo": p + 1,
                    "promedio_notas": round(float(notas.mean()), 2),
                    "asignaturas_perdidas": int((notas < 3.0).sum()),
                    "asistencia": round(asis, 3),
                }
            )

    # Etiqueta: deserción al cierre del año (proceso generador conocido, solo para el prototipo)
    logit = (
        -5.0
        + 1.6 * (3.4 - promedios[:, -1])
        + 6.0 * (0.9 - asistencias[:, -1])
        + 0.35 * perdidas[:, -1]
        + 2.5 * (promedios[:, 0] - promedios[:, -1])
        + 0.45 * extraedad
        + 0.9 * trabaja
        + 0.5 * repitente
        + 0.05 * distancia
        - 0.4 * pae
    )
    prob = 1 / (1 + np.exp(-logit))
    deserta = rng.random(n) < prob

    tabular = pd.DataFrame(
        {
            "estudiante_id": np.arange(n),
            "grado": grado,
            "edad": edad,
            "extraedad": extraedad,
            "estrato": estrato,
            "zona_rural": zona_rural.astype(int),
            "distancia_km": distancia,
            "trabaja": trabaja.astype(int),
            "repitente": repitente.astype(int),
            "beneficiario_pae": pae.astype(int),
            "promedio_notas": promedios[:, -1].round(2),
            "tendencia_notas": (promedios[:, -1] - promedios[:, 0]).round(2),
            "asignaturas_perdidas": perdidas[:, -1],
            "asistencia": asistencias[:, -1].round(3),
            "tendencia_asistencia": (asistencias[:, -1] - asistencias[:, 0]).round(3),
            "deserta": deserta.astype(int),
        }
    )
    return pd.DataFrame(filas_periodo), tabular


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--n", type=int, default=3000)
    parser.add_argument("--semilla", type=int, default=42)
    parser.add_argument("--salida", type=Path, default=Path(__file__).parent / "data")
    args = parser.parse_args()

    args.salida.mkdir(parents=True, exist_ok=True)
    periodos, tabular = generar(args.n, args.semilla)
    periodos.to_csv(args.salida / "estudiantes_periodos.csv", index=False)
    tabular.to_csv(args.salida / "estudiantes_tabular.csv", index=False)
    print(f"Estudiantes: {len(tabular)} | tasa de deserción simulada: {tabular.deserta.mean():.1%}")


if __name__ == "__main__":
    main()
