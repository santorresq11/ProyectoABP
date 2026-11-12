using System.Text.Json;
using System.Text.Json.Serialization;

namespace AulaNube.Api.Application;

/// <summary>
/// Artefacto del modelo exportado por ml/entrenar_modelo_base.py (modelo-riesgo.json).
/// En la arquitectura objetivo este archivo se publica en Azure Blob Storage desde Azure Machine Learning
/// y la API lo carga al iniciar; en el prototipo viaja junto al binario.
/// </summary>
public sealed class ModeloRiesgo
{
    [JsonPropertyName("version")] public required string Version { get; init; }
    [JsonPropertyName("intercepto")] public required double Intercepto { get; init; }
    [JsonPropertyName("umbrales")] public required UmbralesRiesgo Umbrales { get; init; }
    [JsonPropertyName("variables")] public required IReadOnlyList<VariableModelo> Variables { get; init; }

    public static ModeloRiesgo CargarDesdeArchivo(string ruta)
    {
        using var flujo = File.OpenRead(ruta);
        return JsonSerializer.Deserialize<ModeloRiesgo>(flujo)
               ?? throw new InvalidOperationException($"No se pudo leer el modelo en {ruta}");
    }
}

public sealed class UmbralesRiesgo
{
    [JsonPropertyName("medio")] public required double Medio { get; init; }
    [JsonPropertyName("alto")] public required double Alto { get; init; }
}

public sealed class VariableModelo
{
    [JsonPropertyName("nombre")] public required string Nombre { get; init; }
    [JsonPropertyName("descripcion")] public required string Descripcion { get; init; }
    [JsonPropertyName("media")] public required double Media { get; init; }
    [JsonPropertyName("desviacion")] public required double Desviacion { get; init; }
    [JsonPropertyName("peso")] public required double Peso { get; init; }
}
