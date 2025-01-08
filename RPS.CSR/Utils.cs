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

        public static byte[] MifareKeyRepr(string key) {
            var splitted = key.Split(':');
            if (splitted.Length != 6) {
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
}
