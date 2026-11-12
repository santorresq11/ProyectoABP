using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AulaNube.Api.Contracts;
using AulaNube.Api.Domain;
using AulaNube.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AulaNube.Api.Tests;

public class ApiTests(WebApplicationFactory<Program> fabrica) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Health_RespondeOk()
    {
        var respuesta = await fabrica.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task ResumenDelColegio_CuentaTodosLosEstudiantes()
    {
        var resumen = await fabrica.CreateClient()
            .GetFromJsonAsync<ResumenColegioDto>($"/api/colegios/{DatosSemilla.ColegioRural}/resumen", Json);

        Assert.NotNull(resumen);
        Assert.Equal(resumen.TotalEstudiantes, resumen.RiesgoAlto + resumen.RiesgoMedio + resumen.RiesgoBajo);
        Assert.True(resumen.TotalEstudiantes > 0);
    }

    [Fact]
    public async Task EstudiantesDelColegio_VienenOrdenadosPorRiesgo()
    {
        var lista = await fabrica.CreateClient()
            .GetFromJsonAsync<List<EstudianteResumenDto>>($"/api/colegios/{DatosSemilla.ColegioRural}/estudiantes", Json);

        Assert.NotNull(lista);
        Assert.Equal(lista.OrderByDescending(e => e.ProbabilidadRiesgo).Select(e => e.Id), lista.Select(e => e.Id));
    }

    [Fact]
    public async Task ColegioInexistente_Devuelve404()
    {
        var respuesta = await fabrica.CreateClient().GetAsync($"/api/colegios/{Guid.NewGuid()}/resumen");
        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task CrearEstudianteYRegistrarNotas_RecalculaLaAlerta()
    {
        var cliente = fabrica.CreateClient();
        var crear = await cliente.PostAsJsonAsync("/api/estudiantes", new
        {
            colegioId = DatosSemilla.ColegioUrbano,
            nombres = "Estudiante",
            apellidos = "De Prueba",
            documento = "1000000001",
            grado = 9,
            edad = 15,
            estrato = 2,
            distanciaKm = 1.5,
        });
        Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
        var creado = await crear.Content.ReadFromJsonAsync<EstudianteDetalleDto>(Json);
        Assert.NotNull(creado);

        var nota = await cliente.PostAsJsonAsync($"/api/estudiantes/{creado.Id}/calificaciones",
            new { periodo = 1, asignatura = "Matemáticas", nota = 2.1 });
        Assert.Equal(HttpStatusCode.OK, nota.StatusCode);
        var evaluacion = await nota.Content.ReadFromJsonAsync<EvaluacionRiesgo>(Json);
        Assert.NotNull(evaluacion);
        Assert.Equal(creado.Id, evaluacion.EstudianteId);
    }

    [Fact]
    public async Task NotaFueraDeRango_Devuelve400()
    {
        var respuesta = await fabrica.CreateClient().PostAsJsonAsync(
            "/api/estudiantes/5e000000-0000-4000-8000-000000000001/calificaciones",
            new { periodo = 1, asignatura = "Matemáticas", nota = 7.5 });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task InasistenciasMayoresQueClases_Devuelve400()
    {
        var respuesta = await fabrica.CreateClient().PostAsJsonAsync(
            "/api/estudiantes/5e000000-0000-4000-8000-000000000001/asistencias",
            new { periodo = 1, clasesProgramadas = 10, inasistencias = 20 });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
