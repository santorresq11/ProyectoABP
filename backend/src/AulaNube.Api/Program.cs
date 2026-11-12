using System.Text.Json.Serialization;
using AulaNube.Api.Application;
using AulaNube.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "AulaNube API", Version = "v1", Description = "Alerta temprana de deserción escolar para colegios sin infraestructura TI propia." });
    var xml = Path.Combine(AppContext.BaseDirectory, "AulaNube.Api.xml");
    if (File.Exists(xml))
    {
        o.IncludeXmlComments(xml);
    }
});
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

// Telemetría hacia Azure Application Insights (se activa si existe la cadena de conexión).
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// Modelo de IA: ruta configurable (en Azure apunta a una copia descargada de Blob Storage).
var rutaModelo = builder.Configuration["Modelo:Ruta"];
if (string.IsNullOrWhiteSpace(rutaModelo))
{
    rutaModelo = Path.Combine(AppContext.BaseDirectory, "Modelo", "modelo-riesgo.json");
}

builder.Services.AddSingleton(ModeloRiesgo.CargarDesdeArchivo(rutaModelo));
builder.Services.AddSingleton<IServicioRiesgo, ServicioRiesgo>();

// Persistencia: en memoria para el prototipo (Azure SQL en la arquitectura objetivo).
builder.Services.AddSingleton<IRepositorioEscolar>(_ =>
{
    var repo = new RepositorioEnMemoria();
    DatosSemilla.Cargar(repo);
    return repo;
});

var origenes = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
