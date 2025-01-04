using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddDbContext<ApplicationContext>(opts => {
    opts.UseSqlite("Data Source=RPS.CSR.db");
});
builder.Host.UseWindowsService();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetService<ApplicationContext>();
    db?.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.MapControllers();
await app.RunAsync();
