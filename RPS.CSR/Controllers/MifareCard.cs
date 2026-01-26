using System.Net;
using Microsoft.AspNetCore.Mvc;
using RPS.CSR.CardManagement;
using RPS.Devices;
using RPS.Devices.Abstractions;
using RPS.Devices.Mifare;
using System.Globalization;

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
            if (Request.Method == "OPTIONS") {
                return Ok();
            }

            return this.ToJsonp(new {
                Result = new {
                    Version = "3"
                },
                CurrentStatus = WebAnswerT.Succsess,
                Status = WebAnswerT.Succsess,
                Now = DateTimeNow,
            }, callback);
        }

        [HttpGet("SetKey")]
        [HttpOptions("SetKey")]
        public IActionResult SetKey([FromQuery(Name = "key")] string cardKey, [FromQuery] string? callback = null) {
            if (Request.Method == "OPTIONS") {
                return Ok();
            }

            var key = Utils.MifareKeyRepr(cardKey);
            if (key.Length == 0) {
                return this.ToJsonp(new {
                    Result = new {
                        CurrentStatus = "Ошибка парсинга ключа"
                    },
                    Status = WebAnswerT.TemporaryError,
                    CurrentStatus = "Ошибка парсинга ключа",
                    Now = DateTimeNow,
                    IsAuthenticated = false,
                }, callback);
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
                Result = new {
                    CurrentStatus = "Операция выполнена успешно"
                },
                Status = WebAnswerT.Success,
                CurrentStatus = "Операция выполнена успешно",
                Now = DateTimeNow,
                IsAuthenticated = true,
            }, callback);
        }

        [HttpGet("SetSectorNumber")]
        [HttpOptions("SetSectorNumber")]
        public IActionResult SetSectorNumber([FromQuery(Name = "SectorNumber")] int sectorNumber, [FromQuery] string? callback = null) {
            if (Request.Method == "OPTIONS") {
                return Ok();
            }

            if (!Utils.SectorValid(sectorNumber)) {
                return this.ToJsonp(new {
                    Result = new {
                        CurrentStatus = "Номер сектора не верный"
                    },
                    Status = WebAnswerT.TemporaryError,
                    CurrentStatus = "Номер сектора не верный",
                    Now = DateTimeNow,
                    IsAuthenticated = false,
                }, callback);
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
                Result = new {
                    CurrentStatus = "Операция выполнена успешно"
                },
                Status = WebAnswerT.Succsess,
                CurrentStatus = "Операция выполнена успешно",
                Now = DateTimeNow,
                IsAuthenticated = false,
            }, callback);
        }

        [HttpGet("GetData")]
        [HttpOptions("GetData")]
        public async Task<IActionResult> GetData(
            [FromQuery(Name = "sector")] int? querySector,
            [FromQuery] string? cardKey,
            [FromQuery] string? callback = null) {

            if (Request.Method == "OPTIONS") {
                return Ok();
            }

            var key = SelectKey(cardKey);
            var sector = SelectSectorNumber(querySector);

            if (key.Length == 0 || !Utils.SectorValid(sector)) {
                return this.ToJsonp(new {
                    Result = new {
                        CurrentStatus = "Неправильный ключ или сектор",
                        ErrorMessage = "Неправильный ключ или сектор"
                    },
                    Status = WebAnswerT.SomeoneElsesCard,
                    CurrentStatus = "Неправильный ключ или сектор",
                    Now = DateTimeNow,
                }, callback);
            }

            if (this.mifare.DeviceStatus != DeviceConnectionStatus.Connected) {
                return this.ToJsonp(new {
                    Result = new {
                        CurrentStatus = "Ридер не подключен",
                        ErrorMessage = "Ридер не подключен"
                    },
                    Status = WebAnswerT.ReaderNotExists,
                    CurrentStatus = "Ридер не подключен",
                    Now = DateTimeNow,
                }, callback);
            }

            var nuid = await this.mifare.GetCardAsync();
            if (nuid == null || nuid.Length == 0) {
                this.logger.LogInformation("Card not found");
                return this.ToJsonp(new {
                    Result = new {
                        CurrentStatus = "Отсутствует карта!",
                        ErrorMessage = "Отсутствует карта!"
                    },
                    CurrentStatus = "Отсутствует карта!",
                    Status = WebAnswerT.CardNotExists,
                    Now = DateTimeNow,
                }, callback);
            }

            this.logger.LogDebug("Found card with nuid: {nuid}", BitConverter.ToString(nuid));
            var ret = await this.mifare.ReadSectorAsync(nuid, sector, key, null);
            if (ret.Status == ReadStatus.Ok) {
                var card = PhysicalCard.FromMifare(nuid, ret.Data);
                return this.ToJsonp(new {
                    Result = new {
                        Status = "Операция выполнена успешно!", // WebAnswerT.Succsess,

                        CardId = ulong.Parse(BitConverter.ToString(nuid).Replace("-", ""), NumberStyles.HexNumber).ToString(),
                        ParkingEnterTime = card.ParkingEnterTime.ToString(this.dtFormat),
                        LastRecountTime = card.LastRecountTime.ToString(this.dtFormat),
                        SumOnCard = card.SumOnCard.ToString(),
                        TSidFC = card.TSidFC.ToString(),
                        TPidFC = card.TPidFC.ToString(),
                        ZoneidFC = card.ZoneidFC.ToString(),
                        ClientGroupidFC = card.ClientGroupidFC.ToString(),
                        LastPaymentTime = card.LastPaymentTime.ToString(this.dtFormat),
                        Nulltime1 = card.Nulltime1.ToString(this.dtFormat),
                        Nulltime2 = card.Nulltime2.ToString(this.dtFormat),
                        Nulltime3 = card.Nulltime3.ToString(this.dtFormat),
                        TVP = card.TVP.ToString(this.dtFormat),
                        TKVP = card.TKVP.ToString(),
                        ClientTypidFC = card.ClientTypidFC.ToString(),
                        //Regular = false,
                    },
                    CurrentStatus = "Карта прочитана!",
                    Status = WebAnswerT.CardReaded,
                    Now = DateTimeNow,
                }, callback);
            } else {
                this.logger.LogWarning("Cannot read card: {st}", ret.Status);
                return this.ToJsonp(new {
                    Result = new {
                        CurrentStatus = $"Ошибка чтения карты: `{ret.Status}`",
                        ErrorMessage = $"Ошибка чтения карты: `{ret.Status}`"
                    },
                    Status = WebAnswerT.Exception,
                    CurrentStatus = $"Ошибка чтения карты: `{ret.Status}`",
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
            [FromQuery] ulong cardId,
            [FromQuery] DateTime? dateSaveCard,
            [FromQuery(Name = "sector")] int? querySector,
            [FromQuery] string? cardKey,
            [FromQuery] string? callback = null) {

            if (Request.Method == "OPTIONS") {
                return Ok();
            }

            var key = SelectKey(cardKey);
            var sector = SelectSectorNumber(querySector);
            if (key.Length == 0 || !Utils.SectorValid(sector)) {
                return this.ToJsonp(new {
                    Result = new {
                        CurrentStatus = "Неправильный ключ или сектор",
                        ErrorMessage = "Неправильный ключ или сектор"
                    },
                    Status = WebAnswerT.SomeoneElsesCard,
                    CurrentStatus = "Неправильный ключ или сектор",
                    Now = DateTimeNow,
                }, callback);
            }

            if (this.mifare.DeviceStatus != DeviceConnectionStatus.Connected) {
                return this.ToJsonp(new {
                    Result = new {
                        CurrentStatus = "Ридер не подключен",
                        ErrorMessage = "Ридер не подключен"
                    },
                    Status = WebAnswerT.ReaderNotExists,
                    CurrentStatus = "Ридер не подключен",
                    Now = DateTimeNow,
                }, callback);
            }

            var nuid = await this.mifare.GetCardAsync();
            if (nuid == null || nuid.Length == 0) {
                this.logger.LogInformation("Card not found");
                return this.ToJsonp(new {
                    Result = new {
                        CurrentStatus = "Отсутствует карта!",
                        ErrorMessage = "Отсутствует карта!"
                    },
                    CurrentStatus = "Отсутствует карта!",
                    Status = WebAnswerT.CardNotExists,
                    Now = DateTimeNow,
                }, callback);
            }

            var l_nuid = ulong.Parse(BitConverter.ToString(nuid).Replace("-", ""), NumberStyles.HexNumber);// BitConverter.ToString(nuid).Replace("-", "").ToUpper();
            if (l_nuid != cardId) {
                this.logger.LogInformation("Card id mismatch");
                return this.ToJsonp(new {
                    Result = new {
                        CurrentStatus = $"Не совпадает номер карты: запрошен `{cardId}`, найден `{l_nuid}`",
                        ErrorMessage = $"Не совпадает номер карты: запрошен `{cardId}`, найден `{l_nuid}`"
                    },
                    Status = WebAnswerT.TemporaryError,
                    CurrentStatus = $"Card id mismatch: Required `{cardId}`, found `{l_nuid}`",
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
            var s_now = DateTimeNow;
            if (st == WriteStatus.Ok) {
                return this.ToJsonp(new {
                    Result = new {
                        Status = "Операция выполнена успешно!",
                        CardId = cardId.ToString(),
                        ParkingEnterTime = card.ParkingEnterTime.ToString(this.dtFormat),
                        LastRecountTime = card.LastRecountTime.ToString(this.dtFormat),
                        TSidFC = card.TSidFC.ToString(),
                        TPidFC = card.TPidFC.ToString(),
                        ZoneidFC = card.ZoneidFC.ToString(),
                        ClientGroupidFC = card.ClientGroupidFC.ToString(),

                        SumOnCard = card.SumOnCard.ToString(),
                        LastPaymentTime = card.LastPaymentTime.ToString(this.dtFormat),
                        Nulltime1 = card.Nulltime1.ToString(this.dtFormat),
                        Nulltime2 = card.Nulltime2.ToString(this.dtFormat),
                        Nulltime3 = card.Nulltime3.ToString(this.dtFormat),
                        DateSaveCard = s_now,
                        TVP = card.TVP.ToString(this.dtFormat),
                        TKVP = card.TKVP.ToString(),
                        ClientTypidFC = card.ClientTypidFC.ToString(),
                    },
                    Status = WebAnswerT.CardWrited,
                    CurrentStatus = "Карта записана успешно!",
                    Now = s_now,
                }, callback);
            } else {
                this.logger.LogWarning("Cannot write card: {st}", st);
                return this.ToJsonp(new {
                    Result = new {
                        CurrentStatus = $"Ошибка записи карты: `{st}`",
                        ErrorMessage = $"Ошибка записи карты: `{st}`"
                    },
                    Status = WebAnswerT.Exception,
                    CurrentStatus = $"Ошибка записи карты: `{st}`",
                    Now = s_now,
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
