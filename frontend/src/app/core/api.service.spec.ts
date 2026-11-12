import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApiService } from './api.service';

describe('ApiService', () => {
  let servicio: ApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    servicio = TestBed.inject(ApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('pide los estudiantes filtrados por nivel y grado', () => {
    servicio.estudiantes('c1', 'Alto', 9).subscribe((lista) => expect(lista.length).toBe(0));

    const req = http.expectOne((r) => r.url.endsWith('/api/colegios/c1/estudiantes'));
    expect(req.request.params.get('nivel')).toBe('Alto');
    expect(req.request.params.get('grado')).toBe('9');
    req.flush([]);
  });

  it('envía la nota con el formato que espera la API', () => {
    servicio.registrarNota('e1', 2, 'Matemáticas', 2.5).subscribe();

    const req = http.expectOne((r) => r.url.endsWith('/api/estudiantes/e1/calificaciones'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ periodo: 2, asignatura: 'Matemáticas', nota: 2.5 });
    req.flush({});
  });
});
