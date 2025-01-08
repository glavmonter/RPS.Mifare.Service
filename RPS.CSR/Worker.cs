using System.Collections.Concurrent;
using RPS.Devices.Abstractions;
using RPS.Devices.Mifare.Prox;

namespace RPS.CSR {
    public class Worker : BackgroundService {
        private readonly IServiceProvider sp;
        private readonly ILogger<Worker> logger;
        private readonly IMifare mifare;
        private readonly ISerialConnection serial;
        private readonly ConcurrentQueue<object> requestQueue;

        public Worker(IServiceProvider service, IMifare mifare, ISerialConnection serial, ConcurrentQueue<object> requestQueue, ILogger<Worker> logger) {
            this.sp = service;
            this.logger = logger;
            this.mifare = mifare;
            this.serial = serial;
            this.requestQueue = requestQueue;
            if (this.mifare is ProxSerial prox) {
                prox.CheckStatePeriod = TimeSpan.Zero;
            }

            this.mifare.ConnectionStatusChanged += (s, e) => {
                this.logger.LogInformation("Connection changed to '{st}'", e);
            };
        }

        public override async Task StopAsync(CancellationToken cancellationToken) {
            this.serial.Disconnect();
            this.logger.LogInformation("Stopping CardReader service");
            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            OnUpdateSettingsEvent();

            while (!stoppingToken.IsCancellationRequested) {
                if (!this.requestQueue.IsEmpty) {
                    this.requestQueue.TryDequeue(out object? request);
                    switch (request) {
                        case Messages msg:
                            if (msg == Messages.UpdateConfig) {
                                OnUpdateSettingsEvent();
                            }
                            break;
                    }
                } else {
                    await Task.Delay(TimeSpan.FromMilliseconds(10), stoppingToken);
                }
            }
        }

        private void OnUpdateSettingsEvent() {
            this.logger.LogInformation("Update config");
            var settings = new CardReaderSettings();
            settings.Load(this.sp);
            if (!settings.IsValid) {
                this.logger.LogWarning("Settings is Invalid");
                this.serial.Disconnect();
            } else {
                this.logger.LogInformation("Using port '{port}' with speed {speed}", settings.SerialPortName, settings.SerialPortSpeed);
                this.serial.SetPort(settings.SerialPortName, settings.SerialPortSpeed);
                this.serial.Connect();
            }
        }
    }
}
