using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NLog.Web;
using RPS.ConfigurationLoader;
using RPS.CSR;
using RPS.Devices.Abstractions;
using RPS.Devices.Mifare.Prox;
using RPS.Devices.SerialConnection;
using System.Collections.Concurrent;
using System.Reflection;

string appName = Assembly.GetExecutingAssembly().GetName().Name!;

var options = new WebApplicationOptions {
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
};

var builder = WebApplication.CreateBuilder(options);
var logCollector = LogCollector.FromConfiguration(builder.Configuration.GetSection("LogCollector"));
NLogConfigurator.ConfigureTargets(Path.Combine(AppContext.BaseDirectory, "nlog.xml"), appName, logCollector.Port);
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
    var data_source = Path.Combine(AppContext.BaseDirectory, "RPS.CSR.db");
    opts.UseSqlite($"Data Source={data_source}");
});
builder.Host.UseWindowsService();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var conn = db.Database.GetDbConnection();
    await conn.OpenAsync();

    await using (var walCmd = conn.CreateCommand()) {
        walCmd.CommandText = "PRAGMA journal_mode=WAL;";
        await walCmd.ExecuteNonQueryAsync();
    }

    await using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
    var historyExists = await cmd.ExecuteScalarAsync() != null;
   
    await using var tablesCmd = conn.CreateCommand();
    tablesCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
    var hasExistingTables = await tablesCmd.ExecuteScalarAsync() != null;

    if (!historyExists && hasExistingTables) {
        await db.Database.ExecuteSqlRawAsync("""
                                                 CREATE TABLE "__EFMigrationsHistory" (
                                                     "MigrationId" TEXT NOT NULL PRIMARY KEY,
                                                     "ProductVersion" TEXT NOT NULL
                                                 );
                                                 INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                                                 VALUES ('20260116112201_InitialCreate', '9.0.0');
                                             """);
    }

    await db.Database.MigrateAsync();
}

bool useSwagger = app.Configuration.GetValue<bool>("UseSwagger", false);
if (app.Environment.IsDevelopment() || useSwagger) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.MapControllers();
await app.RunAsync();
