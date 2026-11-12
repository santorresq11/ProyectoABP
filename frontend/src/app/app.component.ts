import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { ApiService } from './core/api.service';
import { ColegioActualService } from './core/colegio-actual.service';
import { Colegio } from './core/modelos';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly colegioActual = inject(ColegioActualService);
  readonly colegios = signal<Colegio[]>([]);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.api.colegios().subscribe({
      next: (lista) => {
        this.colegios.set(lista);
        const actual = this.colegioActual.id();
        if (lista.length && !lista.some((c) => c.id === actual)) {
          this.colegioActual.seleccionar(lista[0].id);
        }
      },
      error: () => this.error.set('No se pudo conectar con la API de AulaNube. Verifique que el backend esté en ejecución.'),
    });
  }

  cambiarColegio(evento: Event): void {
    this.colegioActual.seleccionar((evento.target as HTMLSelectElement).value);
  }
}
