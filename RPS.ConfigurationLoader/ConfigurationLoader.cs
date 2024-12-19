// <copyright file="ConfigurationLoader.cs" company="RPS">
// Copyright (c) RPS. All rights reserved.
// </copyright>

using Microsoft.Extensions.Configuration;

namespace RPS.ConfigurationLoader;

/// <summary>
/// Загрузчик конфигурации из JSON
/// </summary>
public static class ConfigurationLoader {
    /// <summary>
    /// Загрузить конфигурацию. Отладочная имеет высший приоритет над рабочей
    /// </summary>
    /// <returns>Интерфейс с конфигурацией или null при ошибках</returns>
    public static IConfiguration? LoadConfiguration() {
        var configuration = LoadDevelopmentConfiguration();
        configuration ??= LoadWorkingConfiguration();
        return configuration;
    }

    /// <summary>
    /// Загрузить отладочную конфигурацию, основанную на переменной окружения TEST_APP_PATH
    /// </summary>
    /// <returns>Интерфейс с конфигурацией или null при ошибках</returns>
    public static IConfiguration? LoadDevelopmentConfiguration() {
        var builder = new ConfigurationBuilder()
            .AddEnvironmentVariables(prefix: "DOTNET_")
            .AddEnvironmentVariables(prefix: "TEST_")
            .AddEnvironmentVariables(prefix: "RPS_");
        return LoadConfigurationImpl(builder, true);
    }

    /// <summary>
    /// Загрузить рабочую конфигурацию, основанную на переменной окружения RPS_APP_PATH
    /// </summary>
    /// <returns>Интерфейс с конфигурацией или null при ошибках</returns>
    public static IConfiguration? LoadWorkingConfiguration() {
        var builder = new ConfigurationBuilder()
            .AddEnvironmentVariables(prefix: "DOTNET_")
            .AddEnvironmentVariables(prefix: "RPS_");
        return LoadConfigurationImpl(builder, false);
    }

    /// <summary>
    /// Реализация загрузки переменных окружения, конфигурационных JSON
    /// </summary>
    /// <param name="builder">Configuration Builder с переменной APP_PATH</param>
    /// <param name="isDevelop">true - для отладочной конфигурации</param>
    /// <returns>Интерфейс с конфигурацией или null при ошибках</returns>
    private static IConfiguration? LoadConfigurationImpl(IConfigurationBuilder builder, bool isDevelop) {
        var inMemoryCollection = new Dictionary<string, string?>() {
            { "isDevelop", isDevelop.ToString() }
        };

        try {
            var configuration = builder.Build();
            var app_path = configuration.GetValue<string>("APP_PATH");
            if (String.IsNullOrEmpty(app_path)) {
                return null;
            }

            string? json_config;
            if (String.Equals(configuration["ENVIRONMENT"], "Development", StringComparison.InvariantCultureIgnoreCase)) {
                json_config = "appsettings.Development.json";
            } else {
                json_config = "appsettings.json";
            }

            inMemoryCollection.Add("logger", Path.Combine(app_path, "Settings", "log4net.config"));
            inMemoryCollection.Add("nlog", Path.Combine(app_path, "Settings", "nlog.xml"));
            inMemoryCollection.Add("app_path", app_path);
            builder.SetBasePath(Path.Combine(app_path, "Settings"))
                .AddJsonFile(json_config)
                .AddInMemoryCollection(inMemoryCollection)
                .AddEnvironmentVariables();
            return builder.Build();
        } catch (Exception) {
            return null;
        }
    }
}
