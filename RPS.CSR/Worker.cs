using RPS.CSR.Models;

namespace RPS.CSR {
    public class Worker : BackgroundService {
        private readonly IServiceProvider sp;
        private readonly ILogger<Worker> logger;

        public Worker(IServiceProvider service, ILogger<Worker> logger) {
            this.sp = service;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            Settings s = Settings;
            if (s.SerialPortSpeed == 0) {
                this.logger.LogWarning("No settings found");
            }

            while (!stoppingToken.IsCancellationRequested) {
                await Task.Delay(1000);
            }
        }

        private Settings Settings {
            get {
                using var scope = this.sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                var s = db.Settings.OrderBy(r => r.Id).FirstOrDefault();
                if (s != null) {
                    return s;
                }
                return new Settings { SerialPortName = "", SerialPortSpeed = 0 };
            }

            set {
                using var scope = this.sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                var s = db.Settings.OrderBy(r => r.Id).LastOrDefault();
                if (s == null) {
                    db.Settings.Add(value);
                } else {
                    s.SerialPortSpeed = value.SerialPortSpeed;
                    s.SerialPortName = value.SerialPortName;
                }
                db.SaveChanges();
            }

        }
    }
}
