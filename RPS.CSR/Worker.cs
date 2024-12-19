namespace RPS.CSR {
    public class Worker : BackgroundService {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                await Task.Delay(1000);
            }
        }
    }
}
