using AulaNube.Api.Application;
using AulaNube.Api.Domain;

namespace AulaNube.Api.Tests;

public class ServicioRiesgoTests
{
    private static readonly ModeloRiesgo Modelo = ModeloRiesgo.CargarDesdeArchivo(
        Path.Combine(AppContext.BaseDirectory, "Modelo", "modelo-riesgo.json"));

    private static Estudiante CrearEstudiante(decimal notaP1, decimal notaP3, int inasistenciasP3, bool trabaja = false, int extraedad = 0)
    {
        var e = new Estudiante
        {
            Id = Guid.NewGuid(),
            ColegioId = Guid.NewGuid(),
            Nombres = "Prueba",
            Apellidos = "Ficticia",
            DocumentoHash = "X",
            Grado = 8,
            Edad = 14 + extraedad,
            Estrato = 1,
            DistanciaKm = 2,
            TrabajaFueraDeClase = trabaja,
            Repitente = false,
            BeneficiarioPae = true,
        };
        foreach (var asignatura in new[] { "Matemáticas", "Lenguaje", "Ciencias" })
        {
            e.Calificaciones.Add(new Calificacion(1, asignatura, notaP1));
            e.Calificaciones.Add(new Calificacion(3, asignatura, notaP3));
        }

        e.Asistencias.Add(new RegistroAsistencia(1, 200, 4));
        e.Asistencias.Add(new RegistroAsistencia(3, 200, inasistenciasP3));
        return e;
    }

    [Fact]
    public void EstudianteConBuenDesempeno_TieneRiesgoBajo()
    {
        var resultado = new ServicioRiesgo(Modelo).Evaluar(CrearEstudiante(4.3m, 4.4m, 4));

        Assert.Equal(NivelRiesgo.Bajo, resultado.Nivel);
        Assert.InRange(resultado.Probabilidad, 0, Modelo.Umbrales.Medio);
    }

    [Fact]
    public void CaidaDeNotasYAsistencia_GeneraRiesgoAltoConFactoresExplicados()
    {
        var resultado = new ServicioRiesgo(Modelo).Evaluar(CrearEstudiante(3.8m, 2.4m, 50, trabaja: true, extraedad: 2));

        Assert.Equal(NivelRiesgo.Alto, resultado.Nivel);
        Assert.NotEmpty(resultado.Factores);
        Assert.All(resultado.Factores, f => Assert.True(f.Contribucion > 0));
        Assert.Contains(resultado.Factores, f => f.Variable is "tendencia_notas" or "promedio_notas" or "asignaturas_perdidas");
        Assert.Contains(resultado.Recomendaciones, r => r.Contains("ruta de permanencia"));
    }

    [Fact]
    public void ElRiesgoAumentaAlEmpeorarLaAsistencia()
    {
        var servicio = new ServicioRiesgo(Modelo);
        var buena = servicio.Evaluar(CrearEstudiante(3.5m, 3.3m, 5));
        var mala = servicio.Evaluar(CrearEstudiante(3.5m, 3.3m, 60));

        Assert.True(mala.Probabilidad > buena.Probabilidad);
    }

    [Fact]
    public void ConstruirVariables_CalculaTendenciasEntrePrimerYUltimoPeriodo()
    {
        var variables = ServicioRiesgo.ConstruirVariables(CrearEstudiante(4.0m, 3.0m, 20, extraedad: 1));

        Assert.Equal(3.0, variables["promedio_notas"], 3);
        Assert.Equal(-1.0, variables["tendencia_notas"], 3);
        Assert.Equal(0.9, variables["asistencia"], 3);
        Assert.Equal(1, variables["extraedad"]);
    }

    [Fact]
    public void EstudianteSinHistorial_NoFalla()
    {
        var e = CrearEstudiante(3m, 3m, 0);
        e.Calificaciones.Clear();
        e.Asistencias.Clear();

        var resultado = new ServicioRiesgo(Modelo).Evaluar(e);

        Assert.InRange(resultado.Probabilidad, 0, 1);
    }
}
