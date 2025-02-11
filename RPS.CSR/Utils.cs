using System;
using System.IO.Ports;

namespace RPS.CSR {
    public enum Messages {
        /// <summary>
        /// Обновить настройки
        /// </summary>
        UpdateConfig
    }

    public static class Utils {
        public static string[] SerialPorts { get {
                return SerialPort.GetPortNames();
            }
        }

        /// <summary>
        /// Преобразование строки ключа их хекса
        /// </summary>
        /// <param name="key">Ключ из 6 хекс знаков. Напрмер ABCD126957AD или AB:CD:12:69:57:AD</param>
        /// <returns>Ключ или пустой массив, если не смог сконвертировать</returns>
        public static byte[] MifareKeyRepr(string key) {
            key = key.Replace(":", "");
            if (key.Length != 12) {
                return [];
            }

            var splitted = key.SplitEveryN(2);
            if (splitted.Count != 6) {
                return [];
            }

            var d = new byte[6];
            try {
                for (int i = 0; i < d.Length; i++) {
                    d[i] = Convert.ToByte(splitted[i], 16);
                }
            } catch {
                return [];
            }

            return d;
        }

        public static bool SectorValid(int sector) {
            return sector >= 1 && sector <= 15;
        }


    }

    public static class StringExtensions {
        public static List<string> SplitEveryN(this string str, int n = 1024) {
            List<string> ret = new List<string>();

            int chunkIterator = 0;
            while (chunkIterator < str.Length) {
                int currentChunkSize = Math.Min(n, str.Length - chunkIterator);
                ret.Add(str.Substring(chunkIterator, currentChunkSize));
                chunkIterator += currentChunkSize;
            }

            return ret;
        }
    }
}
