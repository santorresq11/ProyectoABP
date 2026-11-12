using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AulaNube.Api.Domain;

namespace AulaNube.Api.Infrastructure;

/// <summary>
/// Puerto de persistencia. En el prototipo se implementa en memoria; en Azure se implementa con
/// Entity Framework Core sobre Azure SQL Database (serverless) usando la misma interfaz.
/// </summary>
public interface IRepositorioEscolar
{
    IReadOnlyList<Colegio> ObtenerColegios();
    Colegio? ObtenerColegio(Guid id);
    IReadOnlyList<Estudiante> ObtenerEstudiantes(Guid colegioId);
    Estudiante? ObtenerEstudiante(Guid id);
    Estudiante AgregarEstudiante(Estudiante estudiante);
    void AgregarCalificacion(Guid estudianteId, Calificacion calificacion);
    void RegistrarAsistencia(Guid estudianteId, RegistroAsistencia asistencia);
}

public sealed class RepositorioEnMemoria : IRepositorioEscolar
{
    private readonly ConcurrentDictionary<Guid, Colegio> _colegios = new();
    private readonly ConcurrentDictionary<Guid, Estudiante> _estudiantes = new();
    private readonly object _bloqueo = new();

    public IReadOnlyList<Colegio> ObtenerColegios() => _colegios.Values.OrderBy(c => c.Nombre).ToList();

    public Colegio? ObtenerColegio(Guid id) => _colegios.GetValueOrDefault(id);

    public IReadOnlyList<Estudiante> ObtenerEstudiantes(Guid colegioId) =>
        _estudiantes.Values.Where(e => e.ColegioId == colegioId)
            .OrderBy(e => e.Grado).ThenBy(e => e.Apellidos).ToList();

    public Estudiante? ObtenerEstudiante(Guid id) => _estudiantes.GetValueOrDefault(id);

    public Estudiante AgregarEstudiante(Estudiante estudiante)
    {
        if (!_colegios.ContainsKey(estudiante.ColegioId))
        {
            throw new KeyNotFoundException("El colegio no existe.");
        }

        _estudiantes[estudiante.Id] = estudiante;
        return estudiante;
    }

    public void AgregarColegio(Colegio colegio) => _colegios[colegio.Id] = colegio;

    public void AgregarCalificacion(Guid estudianteId, Calificacion calificacion)
    {
        var e = ObtenerEstudiante(estudianteId) ?? throw new KeyNotFoundException("El estudiante no existe.");
        lock (_bloqueo)
        {
            e.Calificaciones.RemoveAll(c => c.Periodo == calificacion.Periodo && c.Asignatura == calificacion.Asignatura);
            e.Calificaciones.Add(calificacion);
        }
    }

    public void RegistrarAsistencia(Guid estudianteId, RegistroAsistencia asistencia)
    {
        var e = ObtenerEstudiante(estudianteId) ?? throw new KeyNotFoundException("El estudiante no existe.");
        lock (_bloqueo)
        {
            e.Asistencias.RemoveAll(a => a.Periodo == asistencia.Periodo);
            e.Asistencias.Add(asistencia);
        }
    }

    /// <summary>Seudonimiza el documento de identidad: nunca se guarda en claro.</summary>
    public static string Seudonimizar(string documento) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(documento)))[..16];
}
