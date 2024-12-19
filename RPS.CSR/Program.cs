using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting.WindowsServices;
using NLog.Web;
using RPS.ConfigurationLoader;
using RPS.CSR;

string appName = Assembly.GetExecutingAssembly().GetName().Name!;

var options = new WebApplicationOptions {
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
};

var builder = WebApplication.CreateBuilder(options);
var logCollector = LogCollector.FromConfiguration(builder.Configuration.GetSection("LogCollector"));
NLogConfigurator.ConfigureTargets("nlog.xml", appName, logCollector.Port);
NLogConfigurator.ConfigureRules(builder.Configuration.GetSection("Logging"));
if (logCollector.Enable) {
    NLogConfigurator.EnableLogCollectorTarget();
}
NLogConfigurator.Apply();
builder.Logging.ClearProviders();
builder.Host.UseNLog();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<Worker>();
builder.Host.UseWindowsService();

var app = builder.Build();
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.MapControllers();
await app.RunAsync();

//var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddO


//builder.Services.ConfigureHttpJsonOptions(options =>
//{
//    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
//});

//var app = builder.Build();

//var sampleTodos = new Todo[] {
//    new(1, "Walk the dog"),
//    new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
//    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
//    new(4, "Clean the bathroom"),
//    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
//};

//var todosApi = app.MapGroup("/todos");
//todosApi.MapGet("/", () => sampleTodos);
//todosApi.MapGet("/{id}", (int id) =>
//    sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
//        ? Results.Ok(todo)
//        : Results.NotFound());

//app.Run();

//public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

//[JsonSerializable(typeof(Todo[]))]
//internal partial class AppJsonSerializerContext : JsonSerializerContext
//{

//}
