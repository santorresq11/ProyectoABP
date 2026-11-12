import { Injectable, signal } from '@angular/core';

const CLAVE = 'aulanube.colegio';

/** Colegio seleccionado por el usuario (en producción vendría del claim de Entra ID). */
@Injectable({ providedIn: 'root' })
export class ColegioActualService {
  readonly id = signal<string | null>(leer());

  seleccionar(id: string): void {
    this.id.set(id);
    try {
      localStorage.setItem(CLAVE, id);
    } catch {
      /* almacenamiento no disponible: se ignora */
    }
  }
}

function leer(): string | null {
  try {
    return localStorage.getItem(CLAVE);
  } catch {
    return null;
  }
}
