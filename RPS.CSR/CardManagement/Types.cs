namespace RPS.CSR.CardManagement;

public enum ClientType {
    /// <summary>
    /// Неизвестный
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Одноразовый клиент
    /// </summary>
    OneTime = 0,

    /// <summary>
    /// Постоянный клиент
    /// </summary>
    Subscription = 1,

    /// <summary>
    /// Штрафной, работает как разовая.
    /// </summary>
    Penalty = 2,

    /// <summary>
    /// "Вездеход". Как "постоянник", но не проверяется Зона
    /// </summary>
    Unlimited = 3
}
