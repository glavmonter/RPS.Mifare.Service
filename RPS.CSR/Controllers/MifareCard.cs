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

        private string DateTimeNow => DateTime.Now.ToString(this.dtFormat);

        [HttpGet("GetVersion")]
        [HttpOptions("GetVersion")]
        public IActionResult GetVersion([FromQuery] string? callback = null) {
            return this.ToJsonp(new {
                Status = WebAnswerT.Ok,
                Now = DateTimeNow,
            }, callback);
        }

        [HttpGet("SetKey")]
        [HttpOptions("SetKey")]
        public IActionResult SetKey([FromQuery(Name = "key")] string cardKey, [FromQuery] string? callback = null) {
            var key = Utils.MifareKeyRepr(cardKey);
            if (key.Length == 0) {
                return this.ToJsonp(new {
                    Status = WebAnswerT.TemporaryError,
                    CurrentStatus = "key is invalid or invalid format",
                    Now = DateTimeNow,
                }, callback, HttpStatusCode.BadRequest);
            }

            var k = this.db.MifareSettings.OrderBy(r => r.Id).FirstOrDefault();
            if (k == null) {
                k = new Models.MifareSettings { Key = key, SectorNumber = -1 };
                this.db.Add(k);
            } else {
                k.Key = key;
            }

            this.db.SaveChanges();

            return this.ToJsonp(new {
                Status = WebAnswerT.Succsess,
                CurrentStatus = "CardExists",
                Now = DateTimeNow,
                IsAuthenticated = true,
            }, callback);
        }

        [HttpGet("SetSectorNumber")]
        [HttpOptions("SetSectorNumber")]
        public IActionResult SetSectorNumber([FromQuery(Name = "SectorNumber")] int sectorNumber, [FromQuery] string? callback = null) {

            if (!Utils.SectorValid(sectorNumber)) {
                return this.ToJsonp(new {
                    Status = WebAnswerT.TemporaryError,
                    CurrentStatus = "Номер сектора не правильный",
                    Now = DateTimeNow,
                }, callback, HttpStatusCode.BadRequest);
            }

            var k = this.db.MifareSettings.OrderBy(r => r.Id).FirstOrDefault();
            if (k == null) {
                k = new Models.MifareSettings { Key = [], SectorNumber = sectorNumber };
                this.db.Add(k);
            } else {
                k.SectorNumber = sectorNumber;
            }

            this.db.SaveChanges();

            return this.ToJsonp(new {
                Status = WebAnswerT.Succsess,
                CurrentStatus = "Операция выполнена успешно!",
                Now = DateTimeNow,
            }, callback);
        }

        [HttpGet("GetData")]
        [HttpOptions("GetData")]
        public async Task<IActionResult> GetData(
            [FromQuery(Name = "sector")] int? querySector,
            [FromQuery] string? cardKey,
            [FromQuery] string? callback = null) {

            var key = SelectKey(cardKey);
            var sector = SelectSectorNumber(querySector);

            if (key.Length == 0 || !Utils.SectorValid(sector)) {
                return this.ToJsonp(new {
                    Status = WebAnswerT.TemporaryError,
                    CurrentStatus = "Sector or cardKey is invalid",
                    Now = DateTimeNow,
                }, callback, HttpStatusCode.BadRequest);
            }

            if (this.mifare.DeviceStatus != DeviceConnectionStatus.Connected) {
                return this.ToJsonp(new {
                    Status = WebAnswerT.FatalError,
                    CurrentStatus = "Device Not connected or invalid",
                    Now = DateTimeNow,
                }, callback);
            }

            var nuid = await this.mifare.GetCardAsync();
            if (nuid == null || nuid.Length == 0) {
                this.logger.LogInformation("Card not found");
                return this.ToJsonp(new {
                    Status = WebAnswerT.TemporaryError,
                    CurrentStatus = "No card found",
                    Now = DateTimeNow,
                }, callback);
            }

            this.logger.LogDebug("Found card with nuid: {nuid}", BitConverter.ToString(nuid));
            var ret = await this.mifare.ReadSectorAsync(nuid, sector, key, null);
            if (ret.Status == ReadStatus.Ok) {
                var card = PhysicalCard.FromMifare(nuid, ret.Data);
                return this.ToJsonp(new {
                    Status = WebAnswerT.Succsess,
                    CurrentStatus = "Карта прочитана!",
                    Now = DateTimeNow,

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
                    Regular = false,
                }, callback);
            } else {
                this.logger.LogWarning("Cannot read card: {st}", ret.Status);
                return this.ToJsonp(new {
                    Status = WebAnswerT.TemporaryError,
                    CurrentStatus = $"Cannot read card: {ret.Status}",
                    Now = DateTimeNow,
                }, callback);
            }
        }

        [HttpGet("WriteData")]
        [HttpPost("WriteData")]
        [HttpOptions("WriteData")]
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
            [FromQuery(Name = "sector")] int? querySector,
            [FromQuery] string? cardKey,
            [FromQuery] string? callback = null) {

            var key = SelectKey(cardKey);
            var sector = SelectSectorNumber(querySector);
            if (key.Length == 0 || !Utils.SectorValid(sector)) {
                return this.ToJsonp(new {
                    Status = WebAnswerT.TemporaryError,
                    CurrentStatus = "Sector or cardKey is invalid",
                    Now = DateTimeNow,
                }, callback, HttpStatusCode.BadRequest);
            }

            if (this.mifare.DeviceStatus != DeviceConnectionStatus.Connected) {
                return this.ToJsonp(new {
                    Status = WebAnswerT.FatalError,
                    CurrentStatus = "Device Not connected or invalid",
                    Now = DateTimeNow,
                }, callback);
            }

            var nuid = await this.mifare.GetCardAsync();
            if (nuid == null || nuid.Length == 0) {
                this.logger.LogInformation("Card not found");
                return this.ToJsonp(new {
                    Status = WebAnswerT.TemporaryError,
                    CurrentStatus = "No card found",
                    Now = DateTimeNow,
                }, callback);
            }

            var s_nuid = BitConverter.ToString(nuid).Replace("-", "").ToUpper();
            if (!string.Equals(cardId, s_nuid, StringComparison.InvariantCultureIgnoreCase)) {
                this.logger.LogInformation("Card id mismatch");
                return this.ToJsonp(new {
                    Status = WebAnswerT.TemporaryError,
                    CurrentStatus = $"Card id mismatch: Required `{cardId}`, found `{s_nuid}`",
                    Now = DateTimeNow,
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
                    Status = WebAnswerT.Ok,
                    CurrentStatus = "Card writed succesfully",
                    Now = DateTimeNow,
                }, callback);
            } else {
                this.logger.LogWarning("Cannot write card: {st}", st);
                return this.ToJsonp(new {
                    Status = WebAnswerT.TemporaryError,
                    CurrentStatus = $"Cannot write card: {st}",
                    Now = DateTimeNow,
                }, callback);
            }
        }

        private byte[] SelectKey(string? queryKey) {
            var key_query = Utils.MifareKeyRepr(queryKey);
            if (key_query.Length != 0) {
                return key_query;
            }

            var key_db = this.db.MifareSettings.OrderBy(r => r.Id).FirstOrDefault();
            if (key_db == null) {
                return [];
            }

            return key_db.Key;
        }

        private int SelectSectorNumber(int? querySectorNumber) {
            if (querySectorNumber != null) {
                return (int)querySectorNumber;
            }

            var key_db = this.db.MifareSettings.OrderBy(r => r.Id).FirstOrDefault();
            if (key_db == null) {
                return -1;
            }

            return key_db.SectorNumber;
        }
    }
}
