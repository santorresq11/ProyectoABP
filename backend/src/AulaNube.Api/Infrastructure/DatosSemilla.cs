using AulaNube.Api.Domain;

namespace AulaNube.Api.Infrastructure;

/// <summary>
/// Datos ficticios para la demostración del prototipo. Ningún nombre corresponde a una persona real.
/// Se generan con semilla fija para que las evidencias del README sean reproducibles.
/// </summary>
public static class DatosSemilla
{
    private static readonly string[] Nombres =
        ["Valentina", "Santiago", "Mariana", "Juan José", "Isabella", "Samuel", "Sofía", "Matías", "Luciana",
         "Emmanuel", "Salomé", "Jerónimo", "Gabriela", "Thiago", "Daniela", "Miguel Ángel", "Antonella", "Sebastián"];

    private static readonly string[] Apellidos =
        ["Rodríguez", "Gómez", "Martínez", "López", "Hernández", "Díaz", "Moreno", "Rojas", "Vargas", "Castro",
         "Ortiz", "Ramírez", "Suárez", "Cárdenas", "Mosquera", "Quintero", "Ospina", "Arango"];

    private static readonly string[] Asignaturas = ["Matemáticas", "Lenguaje", "Ciencias", "Sociales", "Inglés"];

    public static readonly Guid ColegioRural = Guid.Parse("8f1c2a3e-0000-4000-8000-000000000001");
    public static readonly Guid ColegioUrbano = Guid.Parse("8f1c2a3e-0000-4000-8000-000000000002");

    public static void Cargar(RepositorioEnMemoria repo, int estudiantesPorColegio = 36)
    {
        repo.AgregarColegio(new Colegio(ColegioRural, "I.E. Rural El Porvenir", "219001000101", "Popayán", "Cauca", Zona.Rural, 4));
        repo.AgregarColegio(new Colegio(ColegioUrbano, "I.E. Técnica La Esperanza", "105001000202", "Medellín", "Antioquia", Zona.Urbana, 0));

        var rnd = new Random(2026);
        var consecutivo = 0;
        foreach (var (colegioId, rural) in new[] { (ColegioRural, true), (ColegioUrbano, false) })
        {
            for (var i = 0; i < estudiantesPorColegio; i++)
            {
                consecutivo++;
                var grado = 6 + rnd.Next(0, 6);
                var extraedad = new[] { 0, 0, 0, 0, 1, 1, 2, 3 }[rnd.Next(8)];
                var trabaja = rnd.NextDouble() < (rural ? 0.22 : 0.08);
                var repitente = rnd.NextDouble() < (extraedad > 0 ? 0.25 : 0.05);
                var distancia = rural ? Math.Round(1 + rnd.NextDouble() * 12, 1) : Math.Round(0.3 + rnd.NextDouble() * 2.5, 1);
                var pae = rnd.NextDouble() < 0.7;
                var estrato = rural ? 1 : 1 + rnd.Next(0, 3);

                var estudiante = new Estudiante
                {
                    Id = new Guid($"5e000000-0000-4000-8000-{consecutivo:D12}"),
                    ColegioId = colegioId,
                    Nombres = Nombres[rnd.Next(Nombres.Length)],
                    Apellidos = $"{Apellidos[rnd.Next(Apellidos.Length)]} {Apellidos[rnd.Next(Apellidos.Length)]}",
                    DocumentoHash = RepositorioEnMemoria.Seudonimizar($"DOC-{consecutivo:D6}"),
                    Grado = grado,
                    Edad = grado + 6 + extraedad,
                    Estrato = estrato,
                    DistanciaKm = distancia,
                    TrabajaFueraDeClase = trabaja,
                    Repitente = repitente,
                    BeneficiarioPae = pae,
                };
                repo.AgregarEstudiante(estudiante);

                var vulnerabilidad = 0.6 * extraedad + (trabaja ? 0.9 : 0) + (repitente ? 0.5 : 0)
                                     + 0.08 * distancia - (pae ? 0.3 : 0) + Normal(rnd) * 0.8;
                var notaBase = 3.9 - 0.25 * vulnerabilidad + Normal(rnd) * 0.3;
                var asistenciaBase = 0.95 - 0.03 * vulnerabilidad;
                var deterioro = Math.Clamp(0.05 * vulnerabilidad + Normal(rnd) * 0.08, -0.1, 0.4);

                for (var periodo = 1; periodo <= 3; periodo++)
                {
                    foreach (var asignatura in Asignaturas)
                    {
                        var nota = Math.Clamp(notaBase - deterioro * (periodo - 1) * 1.5 + Normal(rnd) * 0.45, 1.0, 5.0);
                        repo.AgregarCalificacion(estudiante.Id, new Calificacion(periodo, asignatura, Math.Round((decimal)nota, 1)));
                    }

                    var asistencia = Math.Clamp(asistenciaBase - deterioro * (periodo - 1) * 0.12 + Normal(rnd) * 0.02, 0.3, 1.0);
                    const int clases = 200;
                    repo.RegistrarAsistencia(estudiante.Id, new RegistroAsistencia(periodo, clases, (int)Math.Round(clases * (1 - asistencia))));
                }
            }
        }
    }

    private static double Normal(Random rnd)
    {
        // Box-Muller
        var u1 = 1.0 - rnd.NextDouble();
        var u2 = rnd.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}
