namespace RPS.CSR.Models {
    public class Settings {
        public int Id { get; set; }

        public string? SerialPortName { get; set; }

        public int SerialPortSpeed { get; set; }

    }

    public class MifareSettings {
        public int Id { get; set; }

        public byte[] Key { get; set; } = Array.Empty<byte>();

        public int SectorNumber { get; set; }
    }
}
