"""
Modelo objetivo de Deep Learning: red híbrida LSTM + red densa (MLP) para la alerta temprana
de deserción escolar. Se entrena en Azure Machine Learning y se exporta a ONNX para que la API
.NET la ejecute con Microsoft.ML.OnnxRuntime.

Arquitectura:
  rama secuencial  : LSTM(3 -> 32) sobre la serie por periodo [promedio, perdidas/5, asistencia]
  rama tabular     : MLP(8 -> 16) sobre variables de contexto (extraedad, trabajo, distancia, ...)
  cabeza           : concatenación -> Dense(48 -> 16) -> ReLU -> Dropout -> Dense(16 -> 1) -> Sigmoide

Por qué LSTM: el riesgo de deserción no depende solo del estado actual sino de la TRAYECTORIA
(caída de notas y de asistencia periodo a periodo); la memoria de la LSTM captura esa dinámica.

Requiere: pip install -r requirements.txt  (torch, onnx)
Uso:      python train_lstm.py --epocas 30
Estado:   script de prototipo. No se ejecutó en el entorno de desarrollo del sketch (sin GPU/torch);
          la API usa el modelo base (entrenar_modelo_base.py) hasta que este modelo lo supere en ROC-AUC.
"""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
import pandas as pd
import torch
from torch import nn
from torch.utils.data import DataLoader, TensorDataset, random_split

RAIZ = Path(__file__).parent
CONTEXTO = ["extraedad", "repitente", "trabaja", "distancia_km", "estrato", "beneficiario_pae", "zona_rural", "grado"]


class RedDesercion(nn.Module):
    def __init__(self, n_secuencia: int = 3, n_contexto: int = len(CONTEXTO)) -> None:
        super().__init__()
        self.lstm = nn.LSTM(input_size=n_secuencia, hidden_size=32, batch_first=True)
        self.contexto = nn.Sequential(nn.Linear(n_contexto, 16), nn.ReLU())
        self.cabeza = nn.Sequential(nn.Linear(48, 16), nn.ReLU(), nn.Dropout(0.2), nn.Linear(16, 1))

    def forward(self, secuencia: torch.Tensor, contexto: torch.Tensor) -> torch.Tensor:
        _, (h, _) = self.lstm(secuencia)
        z = torch.cat([h[-1], self.contexto(contexto)], dim=1)
        return torch.sigmoid(self.cabeza(z)).squeeze(1)


def cargar_tensores() -> TensorDataset:
    periodos = pd.read_csv(RAIZ / "data" / "estudiantes_periodos.csv").sort_values(["estudiante_id", "periodo"])
    tabular = pd.read_csv(RAIZ / "data" / "estudiantes_tabular.csv").sort_values("estudiante_id")

    n = tabular.shape[0]
    secuencia = np.stack(
        [
            periodos["promedio_notas"].to_numpy() / 5.0,
            periodos["asignaturas_perdidas"].to_numpy() / 5.0,
            periodos["asistencia"].to_numpy(),
        ],
        axis=1,
    ).reshape(n, -1, 3)

    ctx = tabular[CONTEXTO].to_numpy(dtype=float)
    ctx = (ctx - ctx.mean(axis=0)) / ctx.std(axis=0)

    return TensorDataset(
        torch.tensor(secuencia, dtype=torch.float32),
        torch.tensor(ctx, dtype=torch.float32),
        torch.tensor(tabular["deserta"].to_numpy(), dtype=torch.float32),
    )


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--epocas", type=int, default=30)
    parser.add_argument("--lote", type=int, default=64)
    args = parser.parse_args()

    torch.manual_seed(42)
    datos = cargar_tensores()
    n_val = int(0.2 * len(datos))
    entreno, validacion = random_split(datos, [len(datos) - n_val, n_val])
    cargador = DataLoader(entreno, batch_size=args.lote, shuffle=True)

    modelo = RedDesercion()
    optimizador = torch.optim.Adam(modelo.parameters(), lr=1e-3)
    y_tr = datos.tensors[2][entreno.indices]
    peso_pos = (y_tr == 0).sum() / (y_tr == 1).sum()

    for epoca in range(args.epocas):
        modelo.train()
        for seq, ctx, y in cargador:
            p = modelo(seq, ctx)
            pesos = torch.where(y == 1, peso_pos, torch.tensor(1.0))
            perdida = nn.functional.binary_cross_entropy(p, y, weight=pesos)
            optimizador.zero_grad()
            perdida.backward()
            optimizador.step()

        modelo.eval()
        with torch.no_grad():
            seq, ctx, y = (t[validacion.indices] for t in datos.tensors)
            acc = ((modelo(seq, ctx) >= 0.5).float() == y).float().mean().item()
        print(f"época {epoca + 1:02d}  pérdida={perdida.item():.4f}  accuracy_val={acc:.3f}")

    salida = RAIZ / "artefactos" / "modelo_desercion_lstm.onnx"
    salida.parent.mkdir(exist_ok=True)
    seq_ej, ctx_ej, _ = datos[:1]
    torch.onnx.export(
        modelo,
        (seq_ej, ctx_ej),
        salida,
        input_names=["secuencia", "contexto"],
        output_names=["probabilidad"],
        dynamic_axes={"secuencia": {0: "lote"}, "contexto": {0: "lote"}, "probabilidad": {0: "lote"}},
    )
    print(f"Modelo exportado a {salida}")


if __name__ == "__main__":
    main()
