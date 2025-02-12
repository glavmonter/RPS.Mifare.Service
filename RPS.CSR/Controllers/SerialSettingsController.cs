using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using RPS.Devices.SerialConnection;
using System.Net;

namespace RPS.CSR.Controllers {

    public class SerialPortConfig {
        public string SerialPortName { get; set; } = String.Empty;

        public int SerialPortSpeed { get; set; }
    }

    public class AvailablePort {
        public string Name { get; set; } = String.Empty;

        public bool Available { get; set; } = false;
    }

    [ApiController]
    [Route("/[controller]")]
    public class SerialSettingsController : ControllerBase {
        private readonly ILogger<SerialSettingsController> logger;
        private readonly ApplicationDbContext db;
        private readonly ConcurrentQueue<object> requestQueue;

        public SerialSettingsController(ApplicationDbContext db, ConcurrentQueue<object> requestQueue, ILogger<SerialSettingsController> logger) {
            this.logger = logger;
            this.db = db;
            this.requestQueue = requestQueue;
        }

        [HttpGet("GetSerialSettings")]
        public IActionResult GetSerialSettings([FromQuery] string? callback = null) {
            var s = this.db.Settings.OrderBy(r => r.Id).FirstOrDefault();
            if (s == null) {
                return this.ToJsonp(new {
                    Status = "Error",
                    ErrorMessage = "No serial setting found"
                }, callback, HttpStatusCode.OK);
            }

            return this.ToJsonp(new {
                s.SerialPortName,
                s.SerialPortSpeed,
            }, callback);
        }

        [HttpPost("SetSerialSettings")]
        public IActionResult SetSerialSettings([FromBody] SerialPortConfig config, [FromQuery] string? callback = null) {
            if (string.IsNullOrEmpty(config.SerialPortName)) {
                return this.ToJsonp(new {
                    Status = "Error",
                    ErrorMessage = "Invalid argument"
                }, callback, HttpStatusCode.BadRequest);
            }

            var s = this.db.Settings.OrderBy(r => r.Id).FirstOrDefault();
            if (s == null) {
                s = new Models.Settings {
                    SerialPortName = config.SerialPortName,
                    SerialPortSpeed = config.SerialPortSpeed
                };

                this.db.Add(s);
            } else {
                s.SerialPortName = config.SerialPortName;
                s.SerialPortSpeed = config.SerialPortSpeed;
            }

            this.db.SaveChanges();
            this.requestQueue.Enqueue(Messages.UpdateConfig);

            return this.ToJsonp(new {
                Status = "Success",
                Message = "Configured successfully"
            }, callback);
        }

        [HttpGet("AvailablePorts")]
        public IActionResult GetAvailablePorts([FromQuery] string? callback = null) {
            var names = Utils.SerialPorts;
            IList<AvailablePort> ports = new List<AvailablePort>();
            foreach (var p in names) {
                ports.Add(new AvailablePort { Name = p, Available = SerialConnection.CheckPortExists(p) });
            }

            return this.ToJsonp(ports, callback);
        }
    }
}
