import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TableroComponent } from './tablero.component';
import { ColegioActualService } from '../../core/colegio-actual.service';

describe('TableroComponent', () => {
  it('muestra los KPIs y la lista priorizada del colegio seleccionado', () => {
    TestBed.configureTestingModule({
      imports: [TableroComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    TestBed.inject(ColegioActualService).seleccionar('c1');
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(TableroComponent);
    fixture.detectChanges();

    http.expectOne((r) => r.url.endsWith('/colegios/c1/resumen')).flush({
      colegioId: 'c1',
      colegio: 'I.E. de Prueba',
      totalEstudiantes: 2,
      riesgoAlto: 1,
      riesgoMedio: 0,
      riesgoBajo: 1,
      promedioGeneral: 3.4,
      asistenciaPromedio: 0.9,
      porGrado: [{ grado: 9, total: 2, alto: 1, medio: 0 }],
      versionModelo: 'test',
    });
    http.expectOne((r) => r.url.endsWith('/colegios/c1/estudiantes')).flush([
      { id: 'e1', nombre: 'Ana Prueba', grado: 9, edad: 15, promedioActual: 2.6, asistenciaActual: 0.7, nivelRiesgo: 'Alto', probabilidadRiesgo: 0.8 },
    ]);
    fixture.detectChanges();

    const html: HTMLElement = fixture.nativeElement;
    expect(html.querySelector('h1')?.textContent).toContain('I.E. de Prueba');
    expect(html.querySelectorAll('tbody tr').length).toBe(1);
    expect(html.textContent).toContain('Ana Prueba');
  });
});
