namespace RPS.CSR.CardManagement;

/// <summary>
/// Утмлиты для работы с массивами байт
/// </summary>
public static class ByteArrayUtils {
    /// <summary>
    /// Конвертер int в массив 4х byte little endian
    /// </summary>
    /// <param name="d">Входной int</param>
    /// <returns>Массив из 4х байт, младший по младшему адресу</returns>
    public static byte[] FromInt(int d) {
        return HostToLittleEndian(BitConverter.GetBytes(d));
    }

    /// <summary>
    /// Конвертер массива из 4х байт в int
    /// </summary>
    /// <param name="raw">Входной массив из 4х байт Little-Endian</param>
    /// <returns>Целочисленное представление</returns>
    public static int ToInt(byte[] raw) {
        return BitConverter.ToInt32(LittleEndianToHost(raw));
    }

    /// <summary>
    /// Перевод массива в формате ОС в Little-Endian
    /// </summary>
    /// <param name="host">Входной массив в формате ОС</param>
    /// <returns>Выходной массив в формате Little-Endian</returns>
    private static byte[] HostToLittleEndian(byte[] host) {
        if (BitConverter.IsLittleEndian) {
            return host;
        }

        Array.Reverse(host);
        return host;
    }

    /// <summary>
    /// Перевод Little-Endian массива в формат ОС
    /// </summary>
    /// <param name="le">Входной массив в Little-Endian</param>
    /// <returns>Выходной массив в формате ОС</returns>
    private static byte[] LittleEndianToHost(byte[] le) {
        if (BitConverter.IsLittleEndian) {
            return le;
        }

        Array.Reverse(le);
        return le;
    }
}
