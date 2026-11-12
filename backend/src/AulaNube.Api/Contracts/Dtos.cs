using System.ComponentModel.DataAnnotations;
using AulaNube.Api.Domain;

namespace AulaNube.Api.Contracts;

public sealed record EstudianteResumenDto(
    Guid Id,
    string Nombre,
    int Grado,
    int Edad,
    double PromedioActual,
    double AsistenciaActual,
    NivelRiesgo NivelRiesgo,
    double ProbabilidadRiesgo);

public sealed record EstudianteDetalleDto(
    Guid Id,
    Guid ColegioId,
    string Nombre,
    int Grado,
    int Edad,
    int Estrato,
    double DistanciaKm,
    bool TrabajaFueraDeClase,
    bool Repitente,
    bool BeneficiarioPae,
    IReadOnlyList<PeriodoDto> Periodos,
    EvaluacionRiesgo Riesgo);

public sealed record PeriodoDto(int Periodo, double Promedio, int AsignaturasPerdidas, double Asistencia, IReadOnlyList<Calificacion> Notas);

public sealed record ResumenColegioDto(
    Guid ColegioId,
    string Colegio,
    int TotalEstudiantes,
    int RiesgoAlto,
    int RiesgoMedio,
    int RiesgoBajo,
    double PromedioGeneral,
    double AsistenciaPromedio,
    IReadOnlyList<RiesgoPorGradoDto> PorGrado,
    string VersionModelo);

public sealed record RiesgoPorGradoDto(int Grado, int Total, int Alto, int Medio);

public sealed class NuevoEstudianteDto
{
    [Required] public Guid ColegioId { get; init; }
    [Required, StringLength(80, MinimumLength = 2)] public string Nombres { get; init; } = "";
    [Required, StringLength(80, MinimumLength = 2)] public string Apellidos { get; init; } = "";
    [Required, StringLength(20, MinimumLength = 5)] public string Documento { get; init; } = "";
    [Range(0, 11)] public int Grado { get; init; }
    [Range(4, 25)] public int Edad { get; init; }
    [Range(1, 6)] public int Estrato { get; init; }
    [Range(0, 100)] public double DistanciaKm { get; init; }
    public bool TrabajaFueraDeClase { get; init; }
    public bool Repitente { get; init; }
    public bool BeneficiarioPae { get; init; }
}

public sealed class NuevaCalificacionDto
{
    [Range(1, 4)] public int Periodo { get; init; }
    [Required, StringLength(40)] public string Asignatura { get; init; } = "";
    [Range(1.0, 5.0)] public decimal Nota { get; init; }
}

public sealed class NuevaAsistenciaDto
{
    [Range(1, 4)] public int Periodo { get; init; }
    [Range(1, 400)] public int ClasesProgramadas { get; init; }
    [Range(0, 400)] public int Inasistencias { get; init; }
}
