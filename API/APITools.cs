using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using VedAstro.Library;
using Azure;
using Azure.Communication.Email;

namespace API
{
    /// <summary>
    /// API helper utilities for HTTP responses, person list, email, etc.
    /// </summary>
    public static class APITools
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public static HttpResponseData PassMessageJson(object data, HttpRequestData req)
        {
            var payload = new JObject { ["Payload"] = data is JToken jt ? jt : JToken.FromObject(data) };
            var json = payload.ToString();
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Call-Status", "Pass");
            response.Headers.Add("Access-Control-Expose-Headers", "Call-Status");
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(json, Encoding.UTF8);
            return response;
        }

        public static HttpResponseData PassMessageJson(HttpRequestData req)
        {
            var payload = new JObject { ["Payload"] = new JObject() };
            var json = payload.ToString();
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Call-Status", "Pass");
            response.Headers.Add("Access-Control-Expose-Headers", "Call-Status");
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(json, Encoding.UTF8);
            return response;
        }

        public static HttpResponseData FailMessageJson(string message, HttpRequestData req)
        {
            var payload = new JObject { ["Payload"] = message, ["Error"] = message };
            var json = payload.ToString();
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Call-Status", "Fail");
            response.Headers.Add("Access-Control-Expose-Headers", "Call-Status");
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(json, Encoding.UTF8);
            return response;
        }

        public static HttpResponseData FailMessageJson(Exception e, HttpRequestData req)
        {
            return FailMessageJson(e?.Message ?? "Unknown error", req);
        }

        public static async Task<HttpResponseMessage> GetRequest(string url)
        {
            return await HttpClient.GetAsync(url);
        }

        public static HttpResponseData SendAnyToCaller(string calculatorName, object rawProcessedData, HttpRequestData req)
        {
            var jToken = Tools.AnyToJSON(calculatorName, rawProcessedData);
            // AnyToJSON returns JProperty; Payload must be JObject. Wrap JProperty in JObject.
            var payloadContent = jToken is JProperty jp ? new JObject(jp) : jToken;
            var payload = new JObject { ["Payload"] = payloadContent };
            var json = payload.ToString();
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Call-Status", "Pass");
            response.Headers.Add("Access-Control-Expose-Headers", "Call-Status");
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(json, Encoding.UTF8);
            return response;
        }

        public static List<Person> GetAllPersonList(bool skipLifeEvents)
        {
            var rows = AzureTable.PersonList.Query<PersonListEntity>();
            var result = new List<Person>();
            foreach (var row in rows)
            {
                var person = Person.FromAzureRow(row, skipLifeEvents);
                if (!Person.Empty.Equals(person))
                    result.Add(person);
            }
            return result;
        }

        public static HttpResponseData SendTextToCaller(string text, HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Call-Status", "Pass");
            response.Headers.Add("Access-Control-Expose-Headers", "Call-Status");
            response.Headers.Add("Content-Type", "text/html");
            response.WriteString(text, Encoding.UTF8);
            return response;
        }

        public static HttpResponseData SendSvgToCaller(string svg, HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Call-Status", "Pass");
            response.Headers.Add("Access-Control-Expose-Headers", "Call-Status");
            response.Headers.Add("Content-Type", "image/svg+xml");
            response.WriteString(svg, Encoding.UTF8);
            return response;
        }

        public static async Task<JObject> ExtractDataFromRequestJson(HttpRequestData req)
        {
            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();
            return string.IsNullOrEmpty(body) ? new JObject() : JObject.Parse(body);
        }

        public static void SendEmail(string fileName, string extension, string receiverEmail, Stream stream)
        {
            var connectionString = Secrets.Get("EmailConnectionString");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("EmailConnectionString not configured");

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var bytes = ms.ToArray();

            var client = new EmailClient(connectionString);
            var sender = Secrets.Get("EmailSender") ?? "DoNotReply@vedastro.org";
            var content = new EmailContent("VedAstro Chart")
            {
                PlainText = $"Your chart {fileName}.{extension} is attached."
            };
            var attachment = new EmailAttachment($"{fileName}.{extension}", "image/svg+xml", new BinaryData(bytes));
            var message = new EmailMessage(sender, receiverEmail, content);
            message.Attachments.Add(attachment);
            client.Send(WaitUntil.Completed, message);
        }
    }
}
