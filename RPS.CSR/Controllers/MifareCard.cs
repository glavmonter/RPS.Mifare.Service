using Microsoft.AspNetCore.Mvc;

namespace RPS.CSR.Controllers {
    [ApiController]
    [Route("/")]
    public class MifareCard : ControllerBase {
        private readonly ILogger<MifareCard> logger;

        public MifareCard(ILogger<MifareCard> logger) {
            this.logger = logger;
        }

        [HttpPost("WriteData")]
        public IActionResult WriteData(
            [FromQuery] DateTime parkingEnterTime) {

            this.logger.LogInformation("ParkingEnterTime: {pt}", parkingEnterTime);
            return Ok(new {
                Status = "CardWrited",
                Now = DateTime.Now.ToString("o"),
                CurrentStatus = "WriteSuccess"
            });
        }
    }
}
