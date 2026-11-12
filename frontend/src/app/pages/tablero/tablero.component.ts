import { DecimalPipe, PercentPipe } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { ColegioActualService } from '../../core/colegio-actual.service';
import { EstudianteResumen, NivelRiesgo, ResumenColegio } from '../../core/modelos';

@Component({
  selector: 'app-tablero',
  standalone: true,
  imports: [RouterLink, DecimalPipe, PercentPipe],
  templateUrl: './tablero.component.html',
  styleUrl: './tablero.component.scss',
})
export class TableroComponent {
  private readonly api = inject(ApiService);
  private readonly colegioActual = inject(ColegioActualService);

  readonly resumen = signal<ResumenColegio | null>(null);
  readonly estudiantes = signal<EstudianteResumen[]>([]);
  readonly filtroNivel = signal<NivelRiesgo | null>(null);
  readonly filtroGrado = signal<number | null>(null);
  readonly cargando = signal(false);
  readonly niveles: NivelRiesgo[] = ['Alto', 'Medio', 'Bajo'];

  readonly maxPorGrado = computed(() => Math.max(1, ...(this.resumen()?.porGrado.map((g) => g.total) ?? [1])));

  constructor() {
    effect(
      () => {
        const id = this.colegioActual.id();
        if (!id) return;
        this.api.resumen(id).subscribe((r) => this.resumen.set(r));
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
        const id = this.colegioActual.id();
        const nivel = this.filtroNivel();
        const grado = this.filtroGrado();
        if (!id) return;
        this.cargando.set(true);
        this.api.estudiantes(id, nivel, grado).subscribe({
          next: (lista) => {
            this.estudiantes.set(lista);
            this.cargando.set(false);
          },
          error: () => this.cargando.set(false),
        });
      },
      { allowSignalWrites: true },
    );
  }

  alternarNivel(nivel: NivelRiesgo): void {
    this.filtroNivel.update((actual) => (actual === nivel ? null : nivel));
  }

  cambiarGrado(evento: Event): void {
    const valor = (evento.target as HTMLSelectElement).value;
    this.filtroGrado.set(valor ? Number(valor) : null);
  }
}
