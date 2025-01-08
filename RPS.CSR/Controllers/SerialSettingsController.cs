using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using RPS.Devices.SerialConnection;

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
        public IActionResult GetSerialSettings() {
            var s = this.db.Settings.OrderBy(r => r.Id).FirstOrDefault();
            if (s == null) {
                return NotFound(new {
                    Status = "Error",
                    ErrorMessage = "No serial setting found"
                });
            }

            return Ok(new {
                s.SerialPortName,
                s.SerialPortSpeed,
            });
        }

        [HttpPost("SetSerialSettings")]
        public IActionResult SetSerialSettings([FromBody] SerialPortConfig config) {
            if (string.IsNullOrEmpty(config.SerialPortName)) {
                return BadRequest(new {
                    Status = "Error",
                    ErrorMessage = "Invalid argument"
                });
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

            return Ok(new {
                Status = "Success",
                Message = "Configured successfully"
            });
        }

        [HttpGet("AvailablePorts")]
        public IActionResult GetAvailablePorts() {
            var names = Utils.SerialPorts;
            IList<AvailablePort> ports = new List<AvailablePort>();
            foreach (var p in names) {
                ports.Add(new AvailablePort { Name = p, Available = SerialConnection.CheckPortExists(p) });
            }

            return Ok(ports);
        }
    }
}
