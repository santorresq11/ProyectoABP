export type NivelRiesgo = 'Bajo' | 'Medio' | 'Alto';

export interface Colegio {
  id: string;
  nombre: string;
  codigoDane: string;
  municipio: string;
  departamento: string;
  zona: 'Urbana' | 'Rural';
  sedesRurales: number;
}

export interface RiesgoPorGrado {
  grado: number;
  total: number;
  alto: number;
  medio: number;
}

export interface ResumenColegio {
  colegioId: string;
  colegio: string;
  totalEstudiantes: number;
  riesgoAlto: number;
  riesgoMedio: number;
  riesgoBajo: number;
  promedioGeneral: number;
  asistenciaPromedio: number;
  porGrado: RiesgoPorGrado[];
  versionModelo: string;
}

export interface EstudianteResumen {
  id: string;
  nombre: string;
  grado: number;
  edad: number;
  promedioActual: number;
  asistenciaActual: number;
  nivelRiesgo: NivelRiesgo;
  probabilidadRiesgo: number;
}

export interface FactorRiesgo {
  variable: string;
  descripcion: string;
  valor: number;
  contribucion: number;
}

export interface EvaluacionRiesgo {
  estudianteId: string;
  estudiante: string;
  grado: number;
  probabilidad: number;
  nivel: NivelRiesgo;
  factores: FactorRiesgo[];
  recomendaciones: string[];
  versionModelo: string;
}

export interface Calificacion {
  periodo: number;
  asignatura: string;
  nota: number;
}

export interface Periodo {
  periodo: number;
  promedio: number;
  asignaturasPerdidas: number;
  asistencia: number;
  notas: Calificacion[];
}

export interface EstudianteDetalle {
  id: string;
  colegioId: string;
  nombre: string;
  grado: number;
  edad: number;
  estrato: number;
  distanciaKm: number;
  trabajaFueraDeClase: boolean;
  repitente: boolean;
  beneficiarioPae: boolean;
  periodos: Periodo[];
  riesgo: EvaluacionRiesgo;
}
