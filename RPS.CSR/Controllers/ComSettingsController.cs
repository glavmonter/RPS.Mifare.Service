using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using RPS.Devices.SerialConnection;
using System.Net;

namespace RPS.CSR.Controllers {

    public class SerialPortConfig {
        public string Name { get; set; } = String.Empty;

        public int Speed { get; set; }
    }

    public class AvailablePort {
        public string Name { get; set; } = String.Empty;

        public bool Available { get; set; } = false;
    }

    [ApiController]
    [Route("/[controller]")]
    public class ComSettingsController : ControllerBase {
        private readonly ILogger<ComSettingsController> logger;
        private readonly ApplicationDbContext db;
        private readonly ConcurrentQueue<object> requestQueue;

        public ComSettingsController(ApplicationDbContext db, ConcurrentQueue<object> requestQueue, ILogger<ComSettingsController> logger) {
            this.logger = logger;
            this.db = db;
            this.requestQueue = requestQueue;
        }

        [HttpGet("Get")]
        [HttpOptions("Get")]
        public IActionResult GetSerialSettings([FromQuery] string? callback = null) {
            if (Request.Method == "OPTIONS") {
                return Ok();
            }

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

        [HttpGet("Set")]
        [HttpOptions("Set")]
        public IActionResult SetSerialSettings([FromQuery] SerialPortConfig config, [FromQuery] string? callback = null) {
            if (Request.Method == "OPTIONS") {
                return Ok();
            }

            if (string.IsNullOrEmpty(config.Name)) {
                return this.ToJsonp(new {
                    Status = "Error",
                    ErrorMessage = "Invalid argument"
                }, callback, HttpStatusCode.BadRequest);
            }

            var s = this.db.Settings.OrderBy(r => r.Id).FirstOrDefault();
            if (s == null) {
                s = new Models.Settings {
                    SerialPortName = config.Name,
                    SerialPortSpeed = config.Speed
                };

                this.db.Add(s);
            } else {
                s.SerialPortName = config.Name;
                s.SerialPortSpeed = config.Speed;
            }

            this.db.SaveChanges();
            this.requestQueue.Enqueue(Messages.UpdateConfig);

            return this.ToJsonp(new {
                Status = "Success",
                Message = "Configured successfully"
            }, callback);
        }

        [HttpGet("AvailablePorts")]
        [HttpOptions("AvailablePorts")]
        public IActionResult GetAvailablePorts([FromQuery] string? callback = null) {
            if (Request.Method == "OPTIONS") {
                return Ok();
            }

            var names = Utils.SerialPorts;
            IList<AvailablePort> ports = new List<AvailablePort>();
            foreach (var p in names) {
                ports.Add(new AvailablePort { Name = p, Available = SerialConnection.CheckPortExists(p) });
            }

            return this.ToJsonp(ports, callback);
        }
    }
}
