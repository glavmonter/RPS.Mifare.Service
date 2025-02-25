using System.Net;
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
        private readonly ApplicationDbContext db;
        private readonly string dtFormat = "yyyy-MM-dd HH:mm:ss";

        public MifareCard(ApplicationDbContext db, IMifare mifare, ILogger<MifareCard> logger) {
            this.logger = logger;
            this.mifare = mifare;
            this.db = db;
        }

        [HttpGet("GetVersion")]
        [HttpOptions("GetVersion")]
        public IActionResult GetVersion([FromQuery] string? callback = null) {
            return this.ToJsonp(new {
                Status="Ok"
            }, callback);
        }

        [HttpGet("SetKey")]
        [HttpOptions("SetKey")]
        public IActionResult SetKey([FromQuery(Name = "key")] string cardKey, [FromQuery] string? callback = null) {
            var key = Utils.MifareKeyRepr(cardKey);
            if (cardKey.Length == 0) {
                return this.ToJsonp(new {
                    Status = "ArgumentError",
                    Message = "key is invalid or invalid format"
                }, callback, HttpStatusCode.BadRequest);
            }

            var k = this.db.KeySettings.OrderBy(r => r.Id).FirstOrDefault();
            if (k == null) {
                k = new Models.KeySettings { Key = key };
                this.db.Add(k);
            } else {
                k.Key = key;
            }

            this.db.SaveChanges();

            return this.ToJsonp(new {
                Status = "Ok"
            }, callback);
        }

        [HttpGet("GetData")]
        public async Task<IActionResult> GetData(
            [FromQuery] int sector,
            [FromQuery] string? cardKey,
            [FromQuery] string? callback = null) {

            var key = SelectKey(cardKey);
            if (key.Length == 0 || !Utils.SectorValid(sector)) {
                return this.ToJsonp(new {
                    Status = "ArgumentError",
                    Messages = "Sector or cardKey is invalid"
                }, callback, HttpStatusCode.BadRequest);
            }

            if (this.mifare.DeviceStatus != DeviceConnectionStatus.Connected) {
                return this.ToJsonp(new {
                    Status = "NotConnected",
                    Message = "Device Not connected or invalid"
                }, callback);
            }

            var nuid = await this.mifare.GetCardAsync();
            if (nuid == null || nuid.Length == 0) {
                this.logger.LogInformation("Card not found");
                return this.ToJsonp(new {
                    Status = "SomeoneElsesCard",
                    Message = "No card found"
                }, callback);
            }

            this.logger.LogDebug("Found card with nuid: {nuid}", BitConverter.ToString(nuid));
            var ret = await this.mifare.ReadSectorAsync(nuid, sector, key, null);
            if (ret.Status == ReadStatus.Ok) {
                var card = PhysicalCard.FromMifare(nuid, ret.Data);
                return this.ToJsonp(new {
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
                }, callback);
            } else {
                this.logger.LogWarning("Cannot read card: {st}", ret.Status);
                return this.ToJsonp(new {
                    Status = "SomeoneElsesCard",
                    Message = $"Cannot read card: {ret.Status}"
                }, callback);
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
            [FromQuery] string? cardKey,
            [FromQuery] string? callback = null) {

            var key = SelectKey(cardKey);
            if (key.Length == 0 || !Utils.SectorValid(sector)) {
                return this.ToJsonp(new {
                    Status = "ArgumentError",
                    Messages = "Sector or cardKey is invalid"
                }, callback, HttpStatusCode.BadRequest);
            }

            if (this.mifare.DeviceStatus != DeviceConnectionStatus.Connected) {
                return this.ToJsonp(new {
                    Status = "NotConnected",
                    Message = "Device Not connected or invalid"
                }, callback);
            }

            var nuid = await this.mifare.GetCardAsync();
            if (nuid == null || nuid.Length == 0) {
                this.logger.LogInformation("Card not found");
                return this.ToJsonp(new {
                    Status = "SomeoneElsesCard",
                    Message = "No card found"
                }, callback);
            }

            var s_nuid = BitConverter.ToString(nuid).Replace("-", "").ToUpper();
            if (!string.Equals(cardId, s_nuid, StringComparison.InvariantCultureIgnoreCase)) {
                this.logger.LogInformation("Card id mismatch");
                return this.ToJsonp(new {
                    Status = "SomeoneElsesCard",
                    Message = $"Card id mismatch: Required `{cardId}`, found `{s_nuid}`"
                }, callback);
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
                return this.ToJsonp(new {
                    Status = "CardWrited",
                    Now = DateTime.Now.ToString(this.dtFormat),
                    Messages = "Card writed succesfully"
                }, callback);
            } else {
                this.logger.LogWarning("Cannot write card: {st}", st);
                return this.ToJsonp(new {
                    Status = "SomeoneElsesCard",
                    Message = $"Cannot write card: {st}"
                }, callback);
            }
        }

        private byte[] SelectKey(string? queryKey) {
            var key_query = Utils.MifareKeyRepr(queryKey);
            if (key_query.Length != 0) {
                return key_query;
            }

            var key_db = this.db.KeySettings.OrderBy(r => r.Id).FirstOrDefault();
            if (key_db == null) {
                return [];
            }

            return key_db.Key;
        }
    }
}
