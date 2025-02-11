namespace RPS.CSR {
    public class CardReaderSettings {
        public string SerialPortName { get; set; } = String.Empty;

        public int SerialPortSpeed { get; set; }

        public bool IsValid { get => SerialPortName.Length > 0 && SerialPortSpeed > 0; }

        public bool Load(IServiceProvider sp) {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var s = db.Settings.OrderBy(r => r.Id).FirstOrDefault();
            if (s != null) {
                SerialPortName = s.SerialPortName;
                SerialPortSpeed = s.SerialPortSpeed;
            } else {
                SerialPortName = "invalid";
                SerialPortSpeed = 9600;
            }

            return true;
        }

        public void Save(IServiceProvider sp) {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var s = db.Settings.OrderBy(r => r.Id).FirstOrDefault();
            if (s == null) {
                s = new Models.Settings { SerialPortName = SerialPortName, SerialPortSpeed = SerialPortSpeed };
                db.Add(s);
            } else {
                s.SerialPortSpeed = SerialPortSpeed;
                s.SerialPortName = SerialPortName;
            }

            db.SaveChanges();
        }
    }
}
