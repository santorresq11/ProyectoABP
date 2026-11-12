namespace AulaNube.Api.Domain;

public enum NivelRiesgo
{
    Bajo,
    Medio,
    Alto
}

/// <summary>Contribución de una variable al puntaje de riesgo (explicabilidad tipo SHAP simplificada).</summary>
public sealed record FactorRiesgo(string Variable, string Descripcion, double Valor, double Contribucion);

/// <summary>Resultado de la alerta temprana de deserción para un estudiante.</summary>
public sealed record EvaluacionRiesgo(
    Guid EstudianteId,
    string Estudiante,
    int Grado,
    double Probabilidad,
    NivelRiesgo Nivel,
    IReadOnlyList<FactorRiesgo> Factores,
    IReadOnlyList<string> Recomendaciones,
    string VersionModelo);
