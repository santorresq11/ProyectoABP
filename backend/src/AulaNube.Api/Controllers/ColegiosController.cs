using AulaNube.Api.Application;
using AulaNube.Api.Contracts;
using AulaNube.Api.Domain;
using AulaNube.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AulaNube.Api.Controllers;

[ApiController]
[Route("api/colegios")]
[Produces("application/json")]
public sealed class ColegiosController(IRepositorioEscolar repositorio, IServicioRiesgo riesgo) : ControllerBase
{
    /// <summary>Lista los colegios registrados en la plataforma (multi-inquilino).</summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<Colegio>> Listar() => Ok(repositorio.ObtenerColegios());

    /// <summary>Tablero del colegio: distribución de alertas de deserción y promedios.</summary>
    [HttpGet("{colegioId:guid}/resumen")]
    public ActionResult<ResumenColegioDto> Resumen(Guid colegioId)
    {
        var colegio = repositorio.ObtenerColegio(colegioId);
        if (colegio is null)
        {
            return NotFound();
        }

        var estudiantes = repositorio.ObtenerEstudiantes(colegioId);
        var evaluados = estudiantes.Select(e => (Estudiante: e, Riesgo: riesgo.Evaluar(e), Variables: ServicioRiesgo.ConstruirVariables(e))).ToList();

        var porGrado = evaluados
            .GroupBy(x => x.Estudiante.Grado)
            .OrderBy(g => g.Key)
            .Select(g => new RiesgoPorGradoDto(g.Key, g.Count(),
                g.Count(x => x.Riesgo.Nivel == NivelRiesgo.Alto),
                g.Count(x => x.Riesgo.Nivel == NivelRiesgo.Medio)))
            .ToList();

        return Ok(new ResumenColegioDto(
            colegio.Id,
            colegio.Nombre,
            evaluados.Count,
            evaluados.Count(x => x.Riesgo.Nivel == NivelRiesgo.Alto),
            evaluados.Count(x => x.Riesgo.Nivel == NivelRiesgo.Medio),
            evaluados.Count(x => x.Riesgo.Nivel == NivelRiesgo.Bajo),
            evaluados.Count == 0 ? 0 : Math.Round(evaluados.Average(x => x.Variables["promedio_notas"]), 2),
            evaluados.Count == 0 ? 0 : Math.Round(evaluados.Average(x => x.Variables["asistencia"]), 3),
            porGrado,
            riesgo.VersionModelo));
    }

    /// <summary>Estudiantes del colegio con su nivel de alerta, ordenados de mayor a menor riesgo.</summary>
    [HttpGet("{colegioId:guid}/estudiantes")]
    public ActionResult<IReadOnlyList<EstudianteResumenDto>> Estudiantes(Guid colegioId, [FromQuery] NivelRiesgo? nivel = null, [FromQuery] int? grado = null)
    {
        if (repositorio.ObtenerColegio(colegioId) is null)
        {
            return NotFound();
        }

        var lista = repositorio.ObtenerEstudiantes(colegioId)
            .Where(e => grado is null || e.Grado == grado)
            .Select(e =>
            {
                var r = riesgo.Evaluar(e);
                var v = ServicioRiesgo.ConstruirVariables(e);
                return new EstudianteResumenDto(e.Id, e.NombreCompleto, e.Grado, e.Edad,
                    Math.Round(v["promedio_notas"], 2), Math.Round(v["asistencia"], 3), r.Nivel, r.Probabilidad);
            })
            .Where(e => nivel is null || e.NivelRiesgo == nivel)
            .OrderByDescending(e => e.ProbabilidadRiesgo)
            .ToList();

        return Ok(lista);
    }
}
