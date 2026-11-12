import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  Colegio,
  EstudianteDetalle,
  EstudianteResumen,
  EvaluacionRiesgo,
  NivelRiesgo,
  ResumenColegio,
} from './modelos';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api`;

  colegios(): Observable<Colegio[]> {
    return this.http.get<Colegio[]>(`${this.base}/colegios`);
  }

  resumen(colegioId: string): Observable<ResumenColegio> {
    return this.http.get<ResumenColegio>(`${this.base}/colegios/${colegioId}/resumen`);
  }

  estudiantes(colegioId: string, nivel?: NivelRiesgo | null, grado?: number | null): Observable<EstudianteResumen[]> {
    let params = new HttpParams();
    if (nivel) params = params.set('nivel', nivel);
    if (grado) params = params.set('grado', grado);
    return this.http.get<EstudianteResumen[]>(`${this.base}/colegios/${colegioId}/estudiantes`, { params });
  }

  estudiante(id: string): Observable<EstudianteDetalle> {
    return this.http.get<EstudianteDetalle>(`${this.base}/estudiantes/${id}`);
  }

  registrarNota(id: string, periodo: number, asignatura: string, nota: number): Observable<EvaluacionRiesgo> {
    return this.http.post<EvaluacionRiesgo>(`${this.base}/estudiantes/${id}/calificaciones`, { periodo, asignatura, nota });
  }

  registrarAsistencia(id: string, periodo: number, clasesProgramadas: number, inasistencias: number): Observable<EvaluacionRiesgo> {
    return this.http.post<EvaluacionRiesgo>(`${this.base}/estudiantes/${id}/asistencias`, {
      periodo,
      clasesProgramadas,
      inasistencias,
    });
  }
}
