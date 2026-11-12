using AulaNube.Api.Application;
using AulaNube.Api.Contracts;
using AulaNube.Api.Domain;
using AulaNube.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AulaNube.Api.Controllers;

[ApiController]
[Route("api/estudiantes")]
[Produces("application/json")]
public sealed class EstudiantesController(IRepositorioEscolar repositorio, IServicioRiesgo riesgo) : ControllerBase
{
    /// <summary>Ficha del estudiante: historial por periodo y alerta explicada.</summary>
    [HttpGet("{id:guid}")]
    public ActionResult<EstudianteDetalleDto> Obtener(Guid id)
    {
        var e = repositorio.ObtenerEstudiante(id);
        return e is null ? NotFound() : Ok(ADetalle(e));
    }

    /// <summary>Matricula un estudiante. El documento se seudonimiza antes de guardarse.</summary>
    [HttpPost]
    public ActionResult<EstudianteDetalleDto> Crear(NuevoEstudianteDto dto)
    {
        if (repositorio.ObtenerColegio(dto.ColegioId) is null)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(dto.ColegioId)] = ["El colegio no existe."],
            }));
        }

        var estudiante = repositorio.AgregarEstudiante(new Estudiante
        {
            Id = Guid.NewGuid(),
            ColegioId = dto.ColegioId,
            Nombres = dto.Nombres.Trim(),
            Apellidos = dto.Apellidos.Trim(),
            DocumentoHash = RepositorioEnMemoria.Seudonimizar(dto.Documento.Trim()),
            Grado = dto.Grado,
            Edad = dto.Edad,
            Estrato = dto.Estrato,
            DistanciaKm = dto.DistanciaKm,
            TrabajaFueraDeClase = dto.TrabajaFueraDeClase,
            Repitente = dto.Repitente,
            BeneficiarioPae = dto.BeneficiarioPae,
        });

        return CreatedAtAction(nameof(Obtener), new { id = estudiante.Id }, ADetalle(estudiante));
    }

    /// <summary>Registra o actualiza la nota de una asignatura en un periodo y recalcula la alerta.</summary>
    [HttpPost("{id:guid}/calificaciones")]
    public ActionResult<EvaluacionRiesgo> AgregarCalificacion(Guid id, NuevaCalificacionDto dto)
    {
        if (repositorio.ObtenerEstudiante(id) is null)
        {
            return NotFound();
        }

        repositorio.AgregarCalificacion(id, new Calificacion(dto.Periodo, dto.Asignatura.Trim(), Math.Round(dto.Nota, 1)));
        return Ok(riesgo.Evaluar(repositorio.ObtenerEstudiante(id)!));
    }

    /// <summary>Registra la asistencia de un periodo y recalcula la alerta.</summary>
    [HttpPost("{id:guid}/asistencias")]
    public ActionResult<EvaluacionRiesgo> RegistrarAsistencia(Guid id, NuevaAsistenciaDto dto)
    {
        if (dto.Inasistencias > dto.ClasesProgramadas)
        {
            ModelState.AddModelError(nameof(dto.Inasistencias), "Las inasistencias no pueden superar las clases programadas.");
            return ValidationProblem(ModelState);
        }

        if (repositorio.ObtenerEstudiante(id) is null)
        {
            return NotFound();
        }

        repositorio.RegistrarAsistencia(id, new RegistroAsistencia(dto.Periodo, dto.ClasesProgramadas, dto.Inasistencias));
        return Ok(riesgo.Evaluar(repositorio.ObtenerEstudiante(id)!));
    }

    /// <summary>Alerta temprana de deserción del estudiante con sus factores explicativos.</summary>
    [HttpGet("{id:guid}/riesgo")]
    public ActionResult<EvaluacionRiesgo> Riesgo(Guid id)
    {
        var e = repositorio.ObtenerEstudiante(id);
        return e is null ? NotFound() : Ok(riesgo.Evaluar(e));
    }

    private EstudianteDetalleDto ADetalle(Estudiante e)
    {
        var periodos = e.Calificaciones
            .GroupBy(c => c.Periodo)
            .OrderBy(g => g.Key)
            .Select(g => new PeriodoDto(
                g.Key,
                Math.Round((double)g.Average(c => c.Nota), 2),
                g.Count(c => c.Nota < 3.0m),
                Math.Round(e.Asistencias.FirstOrDefault(a => a.Periodo == g.Key)?.PorcentajeAsistencia ?? 1.0, 3),
                g.OrderBy(c => c.Asignatura).ToList()))
            .ToList();

        return new EstudianteDetalleDto(e.Id, e.ColegioId, e.NombreCompleto, e.Grado, e.Edad, e.Estrato, e.DistanciaKm,
            e.TrabajaFueraDeClase, e.Repitente, e.BeneficiarioPae, periodos, riesgo.Evaluar(e));
    }
}
