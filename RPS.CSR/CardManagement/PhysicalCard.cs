using System.Text;

namespace RPS.CSR.CardManagement;

/// <summary>
/// Представление физической карты
/// </summary>
public class PhysicalCard {
    /// <summary>
    /// Время 1 января 2000 года. От этой даты идёт время работы парковки
    /// </summary>
    public static readonly DateTime DateTimeFrom2000 = new DateTime(2000, 1, 1, 0, 0, 0, 0);

    /// <summary>
    /// Тип клиента: <see cref="ClientTypidFC"/>
    /// </summary>
    private byte clientTypeIdFc = 0;
    private ClientType clientType = ClientType.Unknown;

    private byte[] cardUid = Array.Empty<byte>();
    private long id = 0;

    public DateTime ParkingEnterTime { get; set; } = DateTimeFrom2000;

    /// <summary>
    /// Время пересчета денег, тарифов и прочего. Обновляется в калькуляторе
    /// </summary>
    public DateTime LastRecountTime { get; set; } = DateTimeFrom2000;

    /// <summary>
    /// Сумма на карте
    /// </summary>
    public int SumOnCard { get; set; } = 0;

    /// <summary>
    /// Время оплаты
    /// </summary>
    public DateTime LastPaymentTime { get; set; } = DateTimeFrom2000;

    /// <summary>
    /// Время обнуления ВП
    /// </summary>
    public DateTime Nulltime1 { get; set; } = DateTimeFrom2000;

    /// <summary>
    /// Время обнуления КВП
    /// </summary>
    public DateTime Nulltime2 { get; set; } = DateTimeFrom2000;

    /// <summary>
    /// Время обнуления абонемента
    /// </summary>
    public DateTime Nulltime3 { get; set; } = DateTimeFrom2000;

    /// <summary>
    /// Время за период. Клиент заехал, при выезде приплюсовывается длительность нахождения. 
    /// Когда оно превысило определенное значение из тарифной сетки, то меняется тарифный план
    /// в соответсвие с тарифной сеткой. Завязано с Nulltime1.
    /// Переход через Nulltime1 сбрасывает TVP.
    /// Пример: в неделю есть 40 часов бесплатных,
    /// после перехо на платный тариф. Считать должен калькулятор, но не реализовано
    /// </summary>
    public DateTime TVP { get; set; } = DateTimeFrom2000;

    /// <summary>
    /// ТКВП. Количество проездов за преод. За неделю есть 7 въездов бесплатных (есть защитный интервал, ниже которого не засчитывается).
    /// Увелисивает выезд, если выше защитного интервала.
    /// На въезде проверяется превышение и изменяется в калькуляторе (но не точно).
    /// </summary>
    public byte TKVP { get; set; } = 0;

    /// <summary>
    /// Время сохранения карты, хранится на физической карте
    /// </summary>
    public DateTime DateSaveCard { get; set; } = DateTimeFrom2000;

    /// <summary>
    /// Представление типа клиента на физическом носителе. Связан с записью <see cref="ClientType"/>
    /// </summary>
    public byte ClientTypidFC {
        get => this.clientTypeIdFc;
        set {
            this.clientTypeIdFc = value;
            if (Enum.IsDefined(typeof(ClientType), (int)value)) {
                this.clientType = (ClientType)value;
            } else {
                this.clientType = ClientType.Unknown;
            }
        }
    }

    /// <summary>
    /// Тип клиента: разовый, постоянный и тд. Связан с записью <see cref="ClientTypidFC"/>
    /// </summary>
    public ClientType ClientType {
        get => this.clientType;
        set {
            this.clientType = value;
            this.clientTypeIdFc = (byte)value;
        }
    }

    /// <summary>
    /// ID ТС разовый. Для записи на карту (FC - for card)
    /// </summary>
    public byte TSidFC { get; set; } = 0;

    /// <summary>
    /// ID ТП разовый. Для записи на карту (FC - for card)
    /// </summary>
    public byte TPidFC { get; set; } = 0;

    /// <summary>
    /// ID зоны на карте. Для записи на карту (FC - for card). Изменяется на въезде
    /// </summary>
    public byte ZoneidFC { get; set; } = 0;

    /// <summary>
    /// ID зоны на карте. После инициализации из Mifare равна <see cref="ZoneidFC"/>
    /// </summary>
    public byte ZonaDoCard { get; set; } = 0;

    /// <summary>
    /// Флаг блокировки карты. Запись Cards.Blocked == 1 - заблокирована.
    /// </summary>
    public bool IsBlocked { get; set; } = false;

    /// <summary>
    /// Время блокировки из таблицы Cards.BlockTime
    /// </summary>
    public DateTime? BlockTime { get; set; } = null;

    /// <summary>
    /// Номер группы разового клиента. Для записи на карту (FC - for card)
    /// </summary>
    public byte ClientGroupidFC { get; set; } = 0;

    /// <summary>
    /// Номер стойки
    /// </summary>
    public int RackNumber { get; set; }

    /// <summary>
    /// Вычисляемый ID карты <para/>
    /// Для Mifare карт NUID как хекс: 0xAB, 0xCD, 0x65, 0xE8 => 2882364904
    /// </summary>
    public long ID {
        get => this.id;
        set {
            this.id = value;
            this.cardUid = CardUtils.IdToNuid(value);
        }
    }

    /// <summary>
    /// Дата и время создания билета
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Сырые данные карты. Для Mifare данные сектора
    /// </summary>
    public byte[] RawData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Сырой ID карты с Mifare: 4 байта NUID
    /// </summary>
    public byte[] CardUid {
        get => this.cardUid;
        set {
            this.cardUid = value;
            ID = CardUtils.NuidToId(value);
        }
    }

    public override string ToString() {
        var sb = new StringBuilder();
        sb.AppendLine("{Card}");
        sb.AppendLine($"  CardId {this.ID}");
        sb.AppendLine($"  ParkingEnterTime: {ParkingEnterTime}");
        sb.AppendLine($"  DateSaveCard: {DateSaveCard}");
        sb.AppendLine($"  Nulltime1 {Nulltime1}");
        sb.AppendLine($"  Nulltime2 {Nulltime2}");
        sb.AppendLine($"  Nulltime3 {Nulltime3}");
        sb.AppendLine($"  LastPaymentTime {LastPaymentTime}");
        sb.AppendLine($"  LastRecountTime {LastRecountTime}");
        sb.AppendLine($"  TVP {TVP}");
        sb.AppendLine($"  ClientGroupidFC {ClientGroupidFC}");
        sb.AppendLine($"  ClientType: {ClientTypidFC} => {ClientType}");
        sb.AppendLine($"  SumOnCard: {SumOnCard}");
        sb.AppendLine($"  TKVP {TKVP}");
        sb.AppendLine($"  TPidFC {TPidFC}");
        sb.AppendLine($"  TSidFC {TSidFC}");
        sb.Append($"  ZoneidFC {ZoneidFC}");
        return sb.ToString();
    }

    /// <summary>
    /// Перевод времени в локальное, количество секунд с начала 2000 года
    /// </summary>
    /// <param name="normalTime">Нормальное время</param>
    /// <returns>Количество секунд с начала 2000 года до normalTime</returns>
    public static int ToLocalTime(DateTime normalTime) {
        return (int)(normalTime - DateTimeFrom2000).TotalSeconds;
    }

    /// <summary>
    /// Перевод локального времени в нормальное
    /// </summary>
    /// <param name="localTime">Локальное время, количество секунд с начала 2000 года</param>
    /// <returns>Нормальное время</returns>
    public static DateTime FromLocalTime(int localTime) {
        return DateTimeFrom2000.AddSeconds(localTime);
    }

    public static PhysicalCard FromMifare(PhysicalCard card, byte[] nuid, byte[] sector) {
        card.CardUid = nuid;
        card.RawData = sector;
        card.ID = CardUtils.NuidToId(nuid);

        // TODO Проверка сектора на размер!!
        card.ParkingEnterTime = FromLocalTime(ByteArrayUtils.ToInt(sector[0..4]));
        card.LastRecountTime = FromLocalTime(ByteArrayUtils.ToInt(sector[4..8]));
        card.TSidFC = sector[8];
        card.TPidFC = sector[9];
        card.ZoneidFC = sector[10];
        card.ZonaDoCard = card.ZoneidFC;
        card.ClientGroupidFC = sector[11];
        card.SumOnCard = ByteArrayUtils.ToInt(sector[12..16]);
        card.LastPaymentTime = FromLocalTime(ByteArrayUtils.ToInt(sector[16..20]));
        card.Nulltime1 = FromLocalTime(ByteArrayUtils.ToInt(sector[20..24]));
        card.Nulltime2 = FromLocalTime(ByteArrayUtils.ToInt(sector[24..28]));
        card.Nulltime3 = FromLocalTime(ByteArrayUtils.ToInt(sector[28..32]));
        card.TVP = FromLocalTime(ByteArrayUtils.ToInt(sector[32..36]));
        card.TKVP = sector[36];
        card.ClientTypidFC = sector[37];
        card.DateSaveCard = FromLocalTime(ByteArrayUtils.ToInt(sector[44..48]));

        return card;
    }

    public static PhysicalCard FromMifare(byte[] nuid, byte[] sector) {
        var card = new PhysicalCard();
        card.CardUid = nuid;
        card.RawData = sector;
        card.ID = CardUtils.NuidToId(nuid);

        // TODO Проверка сектора на размер!!
        card.ParkingEnterTime = FromLocalTime(ByteArrayUtils.ToInt(sector[0..4]));
        card.LastRecountTime = FromLocalTime(ByteArrayUtils.ToInt(sector[4..8]));
        card.TSidFC = sector[8];
        card.TPidFC = sector[9];
        card.ZoneidFC = sector[10];
        card.ZonaDoCard = card.ZoneidFC;
        card.ClientGroupidFC = sector[11];
        card.SumOnCard = ByteArrayUtils.ToInt(sector[12..16]);
        card.LastPaymentTime = FromLocalTime(ByteArrayUtils.ToInt(sector[16..20]));
        card.Nulltime1 = FromLocalTime(ByteArrayUtils.ToInt(sector[20..24]));
        card.Nulltime2 = FromLocalTime(ByteArrayUtils.ToInt(sector[24..28]));
        card.Nulltime3 = FromLocalTime(ByteArrayUtils.ToInt(sector[28..32]));
        card.TVP = FromLocalTime(ByteArrayUtils.ToInt(sector[32..36]));
        card.TKVP = sector[36];
        card.ClientTypidFC = sector[37];
        card.DateSaveCard = FromLocalTime(ByteArrayUtils.ToInt(sector[44..48]));

        return card;
    }

    /// <summary>
    /// Перевод Карты в сектор Мифар
    /// </summary>
    /// <param name="card"></param>
    /// <returns>Массив бай</returns>
    public static byte[] ToMifare(PhysicalCard card) {
        var list = new List<byte>();
        list.AddRange(CardUtils.FromInt(ToLocalTime(card.ParkingEnterTime)));
        list.AddRange(CardUtils.FromInt(ToLocalTime(card.LastRecountTime)));
        list.Add(card.TSidFC);
        list.Add(card.TPidFC);
        list.Add(card.ZoneidFC);
        list.Add(card.ClientGroupidFC);
        list.AddRange(CardUtils.FromInt(card.SumOnCard));
        list.AddRange(CardUtils.FromInt(ToLocalTime(card.LastPaymentTime)));
        list.AddRange(CardUtils.FromInt(ToLocalTime(card.Nulltime1)));
        list.AddRange(CardUtils.FromInt(ToLocalTime(card.Nulltime2)));
        list.AddRange(CardUtils.FromInt(ToLocalTime(card.Nulltime3)));
        list.AddRange(CardUtils.FromInt(ToLocalTime(card.TVP)));
        list.Add(card.TKVP);
        list.Add(card.ClientTypidFC);
        list.AddRange(new byte[6]);
        list.AddRange(CardUtils.FromInt(ToLocalTime(card.DateSaveCard)));
        return list.ToArray();
    }

    public byte[] ToMifare() {
        return ToMifare(this);
    }
}

public static class CardUtils {
    /// <summary>
    /// Вычисление ID карты: перевод NUID Mifare как хекс в инт. 0xAB 0xCD 0x12 0x56 -> 0xABCD1256
    /// </summary>
    /// <param name="nuid">NUID карты</param>
    /// <returns>ID карты</returns>
    public static long NuidToId(byte[] nuid) {
        long o = 0;
        for (int i = 0; i < nuid.Length; i++) {
            o <<= 8;
            o |= nuid[i];
        }

        return o;
    }

    public static byte[] IdToNuid(long id) {
        return BitConverter.GetBytes(id)[0..4].Reverse().ToArray();
    }

    /// <summary>
    /// Конвертер int в массив 4х byte little endian
    /// </summary>
    /// <param name="d">Входной int</param>
    /// <returns>Массив из 4х байт, младший по младшему адресу</returns>
    public static byte[] FromInt(int d) {
        return HostToLittleEndian(BitConverter.GetBytes(d));
    }

    /// <summary>
    /// Перевод массива в формате ОС в Little-Endian
    /// </summary>
    /// <param name="host">Входной массив в формате ОС</param>
    /// <returns>Выходной массив в формате Little-Endian</returns>
    private static byte[] HostToLittleEndian(byte[] host) {
        if (BitConverter.IsLittleEndian) {
            return host;
        }

        Array.Reverse(host);
        return host;
    }
}
