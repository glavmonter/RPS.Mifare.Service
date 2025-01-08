using Microsoft.AspNetCore.Mvc;
using RPS.CSR.CardManagement;
using RPS.Devices;
using RPS.Devices.Abstractions;
using RPS.Devices.Mifare;

namespace RPS.CSR.Controllers {
    [ApiController]
    [Route("/")]
    public class MifareCard : ControllerBase {
        private readonly ILogger<MifareCard> logger;
        private readonly IMifare mifare;
        private readonly string dtFormat = "yyyy-MM-dd HH:mm:ss";

        public MifareCard(IMifare mifare, ILogger<MifareCard> logger) {
            this.logger = logger;
            this.mifare = mifare;
        }

        [HttpGet("GetData")]
        public async Task<IActionResult> GetData(
            [FromQuery] int sector,
            [FromQuery] string cardKey,
            [FromQuery] string? callback = null) {
            var key = Utils.MifareKeyRepr(cardKey);
            if (key.Length == 0 || !Utils.SectorValid(sector)) {
                return BadRequest(new {
                    Status = "ArgumentError",
                    Messages = "Sector or cardKey is invalid"
                });
            }

            if (this.mifare.DeviceStatus != DeviceConnectionStatus.Connected) {
                return Ok(new {
                    Status = "NotConnected",
                    Message = "Device Not connected or invalid"
                });
            }

            var nuid = await this.mifare.GetCardAsync();
            if (nuid == null || nuid.Length == 0) {
                this.logger.LogInformation("Card not found");
                return Ok(new {
                    Status = "SomeoneElsesCard",
                    Message = "No card found"
                });
            }

            this.logger.LogDebug("Found card with nuid: {nuid}", BitConverter.ToString(nuid));
            var ret = await this.mifare.ReadSectorAsync(nuid, sector, key, null);
            if (ret.Status == ReadStatus.Ok) {
                var card = PhysicalCard.FromMifare(nuid, ret.Data);

                return Ok(new {
                    Status = "CardReaded",
                    CurrentStatus = "NotUsed",
                    Message = "Card readed successfully",
                    Now = DateTime.Now.ToString(this.dtFormat),
                    CardId = BitConverter.ToString(nuid).Replace("-", ""),
                    ParkingEnterTime = card.ParkingEnterTime.ToString(this.dtFormat),
                    LastRecountTime = card.LastRecountTime.ToString(this.dtFormat),
                    SumOnCard = card.SumOnCard,
                    TSidFC = card.TSidFC,
                    TPidFC = card.TPidFC,
                    ZoneidFC = card.ZoneidFC,
                    ClientGroupidFC = card.ClientGroupidFC,
                    LastPaymentTime = card.LastPaymentTime.ToString(this.dtFormat),
                    Nulltime1 = card.Nulltime1.ToString(this.dtFormat),
                    Nulltime2 = card.Nulltime2.ToString(this.dtFormat),
                    Nulltime3 = card.Nulltime3.ToString(this.dtFormat),
                    TVP = card.TVP.ToString(this.dtFormat),
                    TKVP = card.TKVP,
                    Regular = false
                });
            } else {
                this.logger.LogWarning("Cannot read card: {st}", ret.Status);
                return Ok(new {
                    Status = "SomeoneElsesCard",
                    Message = $"Cannot read card: {ret.Status}"
                });
            }
        }

        [HttpPost("WriteData")]
        public async Task<IActionResult> WriteData(
            [FromQuery] DateTime parkingEnterTime,
            [FromQuery] DateTime lastRecountTime,
            [FromQuery] int sumOnCard,
            [FromQuery] int tSidFC,
            [FromQuery] int tPidFC,
            [FromQuery] int zoneidFC,
            [FromQuery] int clientGroupidFC,
            [FromQuery] DateTime lastPaymentTime,
            [FromQuery] DateTime nulltime1,
            [FromQuery] DateTime nulltime2,
            [FromQuery] DateTime nulltime3,
            [FromQuery] DateTime tVP,
            [FromQuery] int clientTypidFC,
            [FromQuery] int tKVP,
            [FromQuery] string cardId,
            [FromQuery] DateTime? dateSaveCard,
            [FromQuery] int sector,
            [FromQuery] string cardKey,
            [FromQuery] string? callback = null) {

            var key = Utils.MifareKeyRepr(cardKey);
            if (key.Length == 0 || !Utils.SectorValid(sector)) {
                return BadRequest(new {
                    Status = "ArgumentError",
                    Messages = "Sector or cardKey is invalid"
                });
            }

            if (this.mifare.DeviceStatus != DeviceConnectionStatus.Connected) {
                return Ok(new {
                    Status = "NotConnected",
                    Message = "Device Not connected or invalid"
                });
            }

            var nuid = await this.mifare.GetCardAsync();
            if (nuid == null || nuid.Length == 0) {
                this.logger.LogInformation("Card not found");
                return Ok(new {
                    Status = "SomeoneElsesCard",
                    Message = "No card found"
                });
            }

            var s_nuid = BitConverter.ToString(nuid).Replace("-", "").ToUpper();
            if (!string.Equals(cardId, s_nuid, StringComparison.InvariantCultureIgnoreCase)) {
                this.logger.LogInformation("Card id mismatch");
                return Ok(new {
                    Status = "SomeoneElsesCard",
                    Message = $"Card id mismatch: Required `{cardId}`, found `{s_nuid}`"
                });
            }

            var card = new PhysicalCard() {
                ParkingEnterTime = parkingEnterTime,
                LastRecountTime = lastRecountTime,
                TSidFC = (byte)tSidFC,
                TPidFC = (byte)tPidFC,
                ZoneidFC = (byte)zoneidFC,
                ClientGroupidFC = (byte)clientGroupidFC,
                SumOnCard = sumOnCard,
                LastPaymentTime = lastPaymentTime,
                Nulltime1 = nulltime1,
                Nulltime2 = nulltime2,
                Nulltime3 = nulltime3,
                TVP = tVP,
                TKVP = (byte)tKVP,
                ClientTypidFC = (byte)clientTypidFC,
                DateSaveCard = dateSaveCard == null ? DateTime.Now : (DateTime)dateSaveCard,
            };

            var wr_data = card.ToMifare();
            var st = await this.mifare.WriteSectorAsync(nuid, sector, key, null, wr_data);
            if (st == WriteStatus.Ok) {
                return Ok(new {
                    Status = "CardWrited",
                    Now = DateTime.Now.ToString(this.dtFormat),
                    Messages = "Card writed succesfully"
                });
            } else {
                this.logger.LogWarning("Cannot write card: {st}", st);
                return Ok(new {
                    Status = "SomeoneElsesCard",
                    Message = $"Cannot write card: {st}"
                });
            }
        }
    }
}
