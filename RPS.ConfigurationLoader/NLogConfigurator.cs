using Microsoft.Extensions.Configuration;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using NLog.Targets.Http;
using NLog.Targets.Wrappers;

namespace RPS.ConfigurationLoader;
public static class NLogConfigurator {
    /// <summary>
    /// Конфигурирование таргетов:
    /// <list type="bullet">
    /// <item>logfile: Вывод в файл logs/appName.log</item>
    /// <item>LogCollector: http+json в http://127.0.0.1:httpPort/log</item>
    /// </list>
    /// </summary>
    /// <param name="nlogConfigFile">Полный путь для файла конфигурации NLog в формате xml</param>
    /// <param name="appName">Имя приложения, например RPS.Application.Configaration</param>
    /// <param name="httpPort">Номер порта для http</param>
    public static void ConfigureTargets(string nlogConfigFile, string appName, int httpPort = 18003) {
        LogManager.Configuration = new XmlLoggingConfiguration(nlogConfigFile);
        if (LogManager.Configuration.FindTargetByName("logfile") is WrapperTargetBase wtb && wtb.WrappedTarget is FileTarget ft) {
            ft.FileName = $"logs/{appName}.log";
            ft.ArchiveFileName = $"logs/{appName}-{{#}}.log";
        }

        if (LogManager.Configuration.FindTargetByName("LogCollector") is AsyncTargetWrapper atw && atw.WrappedTarget is HTTP http && http.Layout is JsonLayout lay) {
            http.Url = Layout.FromString($"http://127.0.0.1:{httpPort}");
            lay.Attributes.Add(new JsonAttribute("_app_", new SimpleLayout(appName)));
        }
    }

    /// <summary>
    /// Настройка правил фильтрации вывода логгера
    /// </summary>
    /// <param name="rules">Секция правил из appsettings.json, Секция Logging</param>
    /// <returns>Конфигурация логгера</returns>
    public static LoggingConfiguration ConfigureRules(IConfigurationSection rules) {
        var cfg = LogManager.Configuration;
        var endRule = cfg.LoggingRules.FirstOrDefault(x => x.LoggerNamePattern == "*");
        if (endRule == default) {
            return cfg;
        }
        cfg.LoggingRules.Clear();

        var logLevelsSection = rules.GetRequiredSection("LogLevel");
        foreach (var c in logLevelsSection.AsEnumerable()) {
            var pattern = c.Key.Split("Logging:LogLevel:"); // Есть префикс секций верхнего уровня: Logging:LogLevel:
            if (pattern.Length > 1) {
                cfg.LoggingRules.Add(CreateFinalRule(pattern[^1], ToNLogLevel(c.Value)));
            }
        }

        cfg.LoggingRules.Add(endRule);
        return cfg;
    }

    /// <summary>
    /// Включить http LogCollector для вывода
    /// </summary>
    public static void EnableLogCollectorTarget() {
        var cfg = LogManager.Configuration;
        var endRule = cfg.FindRuleByName("all");
        if (endRule == null) {
            return;
        }

        var logCollectorTarget = cfg.FindTargetByName("LogCollector");
        if (logCollectorTarget == null) {
            return;
        }

        if (endRule.Targets.FirstOrDefault(x => x.Name == "LogCollector") != default) {
            return;
        }

        endRule.Targets.Add(logCollectorTarget);
    }

    /// <summary>
    /// Применить все принятые настройки
    /// </summary>
    public static void Apply() {
        LogManager.ReconfigExistingLoggers();
    }

    /// <summary>
    /// Создает правило фильтрации для pattern для всех уровней ниже enableLevel.<para/>
    /// Для уровня Info через фильт пройдут все уровни: Critical, Error, Warn и Info. Уровни Debug и Trace будут игнорированы (попадут под правило final)
    /// </summary>
    /// <param name="pattern">Паттерн логгера</param>
    /// <param name="enableLevel">Максимальный уровень логгирования с которого он будет выводиться</param>
    /// <returns>Правило для NLog</returns>
    private static LoggingRule CreateFinalRule(string pattern, LogLevel enableLevel) {
        var rule = new LoggingRule() {
            RuleName = pattern,
            LoggerNamePattern = pattern,
            Final = true
        };
        if (enableLevel > LogLevel.Trace) {
            rule.EnableLoggingForLevels(LogLevel.Trace, LogLevel.FromOrdinal(enableLevel.Ordinal - 1));
        }

        return rule;
    }

    private static LogLevel ToNLogLevel(string? level) {
        return level switch {
            "Trace" => LogLevel.Trace,
            "Debug" => LogLevel.Debug,
            "Information" => LogLevel.Info,
            "Warning" => LogLevel.Warn,
            "Error" => LogLevel.Error,
            "Critical" => LogLevel.Fatal,
            "None" => LogLevel.Off,
            _ => LogLevel.Trace
        };
    }
}
