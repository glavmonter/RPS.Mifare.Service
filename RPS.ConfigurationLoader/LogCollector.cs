using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPS.ConfigurationLoader;

    //"LogCollector": {
    //    "Enable": false,
    //    "BufferSize": 1000,
    //    "FlushTimeout": 5000,
    //    "SlidingTimeout": false,
    //    "DbFile": "CollectorDatabase.db",
    //    "DbCollection": "logs",
    //    "Port": 18003
    //},

/// <summary>
/// Настройки LogCollector. Для клиента валидны Enable и Port. Для сервера валидны все поля, кроме Enable
/// </summary>
public class LogCollector {
    /// <summary>
    /// Включение клиента
    /// </summary>
    public bool Enable { get; private set; } = false;

    /// <summary>
    /// Размер буфера записей сервера
    /// </summary>
    public int BufferSize { get; private set; } = 1000;

    /// <summary>
    /// Таймаут записи на диск, мс
    /// </summary>
    public int FlushTimeout { get; private set; } = 5000;

    /// <summary>
    /// Плавающее время таймаута: true - FlushTimeout стартует после последней записи. При интенсивном логгирование может не произойти никогда
    /// </summary>
    public bool SlidingTimeout { get; private set; } = false;

    /// <summary>
    /// Имя файла хранения базы логов
    /// </summary>
    public string DbFile { get; private set; } = "CollectorDatabase.db";

    /// <summary>
    /// Имя коллекции логов в базе данных
    /// </summary>
    public string DbCollection { get; private set; } = "logs";

    /// <summary>
    /// Номер порта сервиса коллектора
    /// </summary>
    public int Port { get; private set; } = 18003;

    public static LogCollector FromConfiguration(IConfigurationSection config) {
        var lc = new LogCollector {
            Enable = config.GetValue("Enable", false),
            BufferSize = config.GetValue("BufferSize", 1000),
            FlushTimeout = config.GetValue("FlushTimeout", 5000),
            SlidingTimeout = config.GetValue("SlidingTimeout", false),
            DbFile = config.GetValue("DbFile", "CollectorDatabase.db")!,
            DbCollection = config.GetValue("DbCollection", "logs")!,
            Port = config.GetValue("Port", 18003)
        };
        return lc;
    }
}
