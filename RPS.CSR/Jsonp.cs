using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace RPS.CSR {
    public static class Jsonp {
        public static ContentResult ToJsonp(this ControllerBase controller, object data, string? callback, HttpStatusCode statusCode = HttpStatusCode.OK) {
            controller.Response.StatusCode = (int)statusCode;
            var result = new {
                Result = data,
            };

            string json = JsonConvert.SerializeObject(result);
            string response = string.IsNullOrEmpty(callback) ? json : $"{callback}({json})";
            string type = string.IsNullOrEmpty(callback) ? "application/json" : "application/javascript";
            return controller.Content(response, type);
        }
    }
}
