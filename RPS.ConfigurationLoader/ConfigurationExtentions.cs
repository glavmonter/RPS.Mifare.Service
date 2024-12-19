// <copyright file="ConfigurationExtentions.cs" company="RPS">
// Copyright (c) RPS. All rights reserved.
// </copyright>

using Microsoft.Extensions.Configuration;
using RPS.ConfigurationLoader.Exceptions;

namespace RPS.ConfigurationLoader;
public static class ConfigurationExtentions {
    public static string RabbitMqUser(this IConfiguration? configuration) {
        if (configuration == null) {
            throw new ArgumentNullException(nameof(configuration), "Argumet is null");
        }

        CheckSectionRabbitMq(configuration);
        var user = configuration.GetSection("CustomConnectionStrings").GetSection("rabbitmq").GetValue<string>("user");
        return user ?? throw new ConfigurationException("RabbitMq username not set or invalid");
    }

    public static string RabbitMqPassword(this IConfiguration? configuration) {
        if (configuration == null) {
            throw new ArgumentNullException(nameof(configuration), "Argumet is null");
        }

        CheckSectionRabbitMq(configuration);
        var pass = configuration.GetSection("CustomConnectionStrings").GetSection("rabbitmq").GetValue<string>("password");
        return pass ?? throw new ConfigurationException("RabbitMq password not set or invalid");
    }

    public static string RabbitMqHost(this IConfiguration? configuration) {
        if (configuration == null) {
            throw new ArgumentNullException(nameof(configuration), "Argumet is null");
        }

        CheckSectionRabbitMq(configuration);
        var host = configuration.GetSection("CustomConnectionStrings").GetSection("rabbitmq").GetValue<string>("host");
        return host ?? throw new ConfigurationException("RabbitMq host not set or invalid");
    }

    public static ushort RabbitMqPort(this IConfiguration? configuration) {
        if (configuration == null) {
            throw new ArgumentNullException(nameof(configuration), "Argumet is null");
        }

        CheckSectionRabbitMq(configuration);
        var port = configuration.GetSection("CustomConnectionStrings").GetSection("rabbitmq").GetValue<int>("port", -1);
        if (port < 0) {
            throw new ConfigurationException("RabbitMq port not set or invalid");
        }
        return (ushort)port;
    }

    private static void CheckSectionRabbitMq(IConfiguration configuration) {
        try {
            configuration.GetRequiredSection("CustomConnectionStrings").GetRequiredSection("rabbitmq");
        } catch (Exception ex) {
            throw new ConfigurationException($"Section: CustomConnectionStrings->rabbitmq not satisfied: `{ex.Message}`");
        }
    }
}
