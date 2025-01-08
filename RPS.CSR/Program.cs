using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NLog.Web;
using RPS.ConfigurationLoader;
using RPS.CSR;
using RPS.Devices.Abstractions;
using RPS.Devices.Mifare.Prox;
using RPS.Devices.SerialConnection;

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
builder.Services.AddSingleton<ISerialConnection, SerialConnection>();
builder.Services.AddSingleton<IMifare>(sp => {
    var c = sp.GetRequiredService<ISerialConnection>();
    var mifare = new ProxSerial(c, sp.GetRequiredService<ILogger<ProxSerial>>()) {
        DelayBetweenRead = TimeSpan.FromMilliseconds(10),
        DelayBetweenWrite = TimeSpan.FromMilliseconds(10)
    };
    return mifare;
});
builder.Services.AddSingleton<ConcurrentQueue<object>>(sp => {
    return new ConcurrentQueue<object>();
});
builder.Services.AddHostedService<Worker>();
builder.Services.AddDbContext<ApplicationDbContext>(opts => {
    opts.UseSqlite("Data Source=RPS.CSR.db");
});
builder.Host.UseWindowsService();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetService<ApplicationDbContext>();
    db?.Database.EnsureCreated();
}

bool useSwagger = app.Configuration.GetValue<bool>("UseSwagger", false);
if (app.Environment.IsDevelopment() || useSwagger) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.MapControllers();
await app.RunAsync();
