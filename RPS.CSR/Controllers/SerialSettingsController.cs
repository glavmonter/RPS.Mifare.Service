using Microsoft.AspNetCore.Mvc;

namespace RPS.CSR.Controllers {

    public class SerialPortConfig {
        public string SerialPortName { get; set; } = String.Empty;
        public int SerialPortSpeed { get; set; }
    }

    [ApiController]
    [Route("/[controller]")]
    public class SerialSettingsController : ControllerBase {
        private readonly ILogger<SerialSettingsController> logger;
        private readonly ApplicationContext db;

        public SerialSettingsController(ApplicationContext db, ILogger<SerialSettingsController> logger) {
            this.logger = logger;
            this.db = db;
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
                SerialPortName = s.SerialPortName,
                SerialPortSpeed = s.SerialPortSpeed,
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

            return Ok(new {
                Status = "Success",
                Message = "Configured successfully"
            });
        }
    }
}
