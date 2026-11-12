using AulaNube.Api.Domain;

namespace AulaNube.Api.Application;

public interface IServicioRiesgo
{
    string VersionModelo { get; }
    EvaluacionRiesgo Evaluar(Estudiante estudiante);
}

/// <summary>
/// Motor de inferencia de la alerta temprana. Convierte el historial del estudiante en el vector de
/// variables del modelo, calcula la probabilidad de deserción y explica qué variables la empujan.
/// El resultado es un APOYO a la decisión del docente/orientador, nunca una decisión automática.
/// </summary>
public sealed class ServicioRiesgo(ModeloRiesgo modelo) : IServicioRiesgo
{
    public string VersionModelo => modelo.Version;

    public EvaluacionRiesgo Evaluar(Estudiante estudiante)
    {
        var variables = ConstruirVariables(estudiante);

        var factores = new List<FactorRiesgo>();
        var z = modelo.Intercepto;
        foreach (var v in modelo.Variables)
        {
            var valor = variables.TryGetValue(v.Nombre, out var x) ? x : v.Media;
            var contribucion = v.Peso * (valor - v.Media) / v.Desviacion;
            z += contribucion;
            factores.Add(new FactorRiesgo(v.Nombre, v.Descripcion, Math.Round(valor, 3), Math.Round(contribucion, 3)));
        }

        var probabilidad = 1.0 / (1.0 + Math.Exp(-z));
        var nivel = probabilidad >= modelo.Umbrales.Alto ? NivelRiesgo.Alto
            : probabilidad >= modelo.Umbrales.Medio ? NivelRiesgo.Medio
            : NivelRiesgo.Bajo;

        var principales = factores
            .Where(f => f.Contribucion > 0)
            .OrderByDescending(f => f.Contribucion)
            .Take(3)
            .ToList();

        return new EvaluacionRiesgo(
            estudiante.Id,
            estudiante.NombreCompleto,
            estudiante.Grado,
            Math.Round(probabilidad, 4),
            nivel,
            principales,
            Recomendar(nivel, principales),
            modelo.Version);
    }

    /// <summary>Ingeniería de características: mismas definiciones que ml/generar_datos_sinteticos.py.</summary>
    public static Dictionary<string, double> ConstruirVariables(Estudiante e)
    {
        var promedios = e.Calificaciones
            .GroupBy(c => c.Periodo)
            .OrderBy(g => g.Key)
            .Select(g => new { Periodo = g.Key, Promedio = (double)g.Average(c => c.Nota), Perdidas = g.Count(c => c.Nota < 3.0m) })
            .ToList();
        var asistencias = e.Asistencias.OrderBy(a => a.Periodo).ToList();

        var ultimo = promedios.LastOrDefault();
        var primero = promedios.FirstOrDefault();

        return new Dictionary<string, double>
        {
            ["promedio_notas"] = ultimo?.Promedio ?? 3.5,
            ["tendencia_notas"] = ultimo is null || primero is null ? 0 : ultimo.Promedio - primero.Promedio,
            ["asignaturas_perdidas"] = ultimo?.Perdidas ?? 0,
            ["asistencia"] = asistencias.LastOrDefault()?.PorcentajeAsistencia ?? 1.0,
            ["tendencia_asistencia"] = asistencias.Count < 2 ? 0
                : asistencias[^1].PorcentajeAsistencia - asistencias[0].PorcentajeAsistencia,
            ["extraedad"] = e.Extraedad,
            ["repitente"] = e.Repitente ? 1 : 0,
            ["trabaja"] = e.TrabajaFueraDeClase ? 1 : 0,
            ["distancia_km"] = e.DistanciaKm,
            ["estrato"] = e.Estrato,
            ["beneficiario_pae"] = e.BeneficiarioPae ? 1 : 0,
        };
    }

    private static IReadOnlyList<string> Recomendar(NivelRiesgo nivel, IReadOnlyList<FactorRiesgo> principales)
    {
        if (nivel == NivelRiesgo.Bajo)
        {
            return ["Seguimiento regular del director de grupo."];
        }

        var acciones = new List<string>();
        foreach (var f in principales)
        {
            var accion = f.Variable switch
            {
                "promedio_notas" or "tendencia_notas" or "asignaturas_perdidas" =>
                    "Plan de nivelación académica con los docentes de las asignaturas en riesgo.",
                "asistencia" or "tendencia_asistencia" =>
                    "Contactar a la familia para indagar las causas de las inasistencias.",
                "trabaja" => "Remitir a orientación escolar para revisar la carga laboral del estudiante.",
                "distancia_km" => "Verificar acceso a transporte escolar o modelos flexibles de la sede rural.",
                "extraedad" or "repitente" => "Evaluar ingreso a un modelo educativo flexible (aceleración del aprendizaje).",
                "beneficiario_pae" => "Verificar inclusión en el Programa de Alimentación Escolar (PAE).",
                _ => null,
            };
            if (accion is not null && !acciones.Contains(accion))
            {
                acciones.Add(accion);
            }
        }

        if (nivel == NivelRiesgo.Alto)
        {
            acciones.Add("Activar la ruta de permanencia escolar con orientación y coordinación en los próximos 8 días.");
        }

        return acciones;
    }
}
