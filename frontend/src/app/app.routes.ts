import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/tablero/tablero.component').then((m) => m.TableroComponent) },
  {
    path: 'estudiantes/:id',
    loadComponent: () => import('./pages/estudiante/estudiante.component').then((m) => m.EstudianteComponent),
  },
  { path: '**', redirectTo: '' },
];
