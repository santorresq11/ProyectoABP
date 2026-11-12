import { DecimalPipe, PercentPipe } from '@angular/common';
import { Component, OnChanges, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { ETIQUETAS_VARIABLES } from '../../core/etiquetas';
import { EstudianteDetalle, Periodo } from '../../core/modelos';

@Component({
  selector: 'app-estudiante',
  standalone: true,
  imports: [RouterLink, DecimalPipe, PercentPipe, FormsModule],
  templateUrl: './estudiante.component.html',
  styleUrl: './estudiante.component.scss',
})
export class EstudianteComponent implements OnChanges {
  private readonly api = inject(ApiService);

  /** Parámetro de ruta :id (withComponentInputBinding). */
  readonly id = input.required<string>();

  readonly detalle = signal<EstudianteDetalle | null>(null);
  readonly mensaje = signal<string | null>(null);
  readonly etiquetas = ETIQUETAS_VARIABLES;
  readonly asignaturas = ['Matemáticas', 'Lenguaje', 'Ciencias', 'Sociales', 'Inglés'];

  nota = { periodo: 4, asignatura: 'Matemáticas', valor: 3.0 };
  asistencia = { periodo: 4, clases: 200, inasistencias: 10 };

  readonly maxContribucion = computed(() =>
    Math.max(0.01, ...(this.detalle()?.riesgo.factores.map((f) => f.contribucion) ?? [0.01])),
  );

  ngOnChanges(): void {
    this.cargar();
  }

  cargar(): void {
    this.api.estudiante(this.id()).subscribe((d) => this.detalle.set(d));
  }

  guardarNota(): void {
    this.api.registrarNota(this.id(), this.nota.periodo, this.nota.asignatura, this.nota.valor).subscribe({
      next: (r) => this.trasGuardar(`Nota registrada. Nueva probabilidad: ${(r.probabilidad * 100).toFixed(0)} % (${r.nivel}).`),
      error: () => this.mensaje.set('No se pudo registrar la nota. Revise que esté entre 1.0 y 5.0.'),
    });
  }

  guardarAsistencia(): void {
    this.api
      .registrarAsistencia(this.id(), this.asistencia.periodo, this.asistencia.clases, this.asistencia.inasistencias)
      .subscribe({
        next: (r) => this.trasGuardar(`Asistencia registrada. Nueva probabilidad: ${(r.probabilidad * 100).toFixed(0)} % (${r.nivel}).`),
        error: () => this.mensaje.set('No se pudo registrar la asistencia. Revise los valores.'),
      });
  }

  notaDe(periodo: Periodo, asignatura: string): number | undefined {
    return periodo.notas.find((x) => x.asignatura === asignatura)?.nota;
  }

  private trasGuardar(texto: string): void {
    this.mensaje.set(texto);
    this.cargar();
  }
}
