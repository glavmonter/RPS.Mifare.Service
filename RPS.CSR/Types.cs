using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RPS.CSR {
    [JsonConverter(typeof(StringEnumConverter))]
    public enum WebAnswerT {
        Undefined,
        Succsess,
        TemporaryError,
        FatalError,
        Ok
    }
}
