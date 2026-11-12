namespace AulaNube.Api.Domain;

/// <summary>Institución educativa (colegio) que usa la plataforma como servicio.</summary>
public sealed record Colegio(
    Guid Id,
    string Nombre,
    string CodigoDane,
    string Municipio,
    string Departamento,
    Zona Zona,
    int SedesRurales);

public enum Zona
{
    Urbana,
    Rural
}

/// <summary>
/// Estudiante matriculado. Solo guarda los datos mínimos necesarios para la alerta temprana
/// (principio de minimización de datos, Ley 1581 de 2012). El documento se guarda seudonimizado.
/// </summary>
public sealed class Estudiante
{
    public required Guid Id { get; init; }
    public required Guid ColegioId { get; init; }
    public required string Nombres { get; init; }
    public required string Apellidos { get; init; }
    public required string DocumentoHash { get; init; }
    public required int Grado { get; init; }
    public required int Edad { get; init; }
    public required int Estrato { get; init; }
    public required double DistanciaKm { get; init; }
    public required bool TrabajaFueraDeClase { get; init; }
    public required bool Repitente { get; init; }
    public required bool BeneficiarioPae { get; init; }

    public List<Calificacion> Calificaciones { get; } = new();
    public List<RegistroAsistencia> Asistencias { get; } = new();

    public string NombreCompleto => $"{Nombres} {Apellidos}";

    /// <summary>Años por encima de la edad esperada para el grado (grado + 6, misma regla que ml/).</summary>
    public int Extraedad => Math.Max(0, Edad - (Grado + 6));
}

/// <summary>Nota de un estudiante en una asignatura y periodo académico (escala 1.0 a 5.0).</summary>
public sealed record Calificacion(int Periodo, string Asignatura, decimal Nota);

/// <summary>Resumen de asistencia de un estudiante en un periodo académico.</summary>
public sealed record RegistroAsistencia(int Periodo, int ClasesProgramadas, int Inasistencias)
{
    public double PorcentajeAsistencia =>
        ClasesProgramadas == 0 ? 1.0 : 1.0 - (double)Inasistencias / ClasesProgramadas;
}
