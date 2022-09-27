using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace Leaf2Google.Helpers
{
    public static class Request
    {
        public static async Task<Response> MakeRequest(this HttpClient client, HttpRequestMessage httpRequestMessage)
        {
            var httpResponseMessage = await client.SendAsync(httpRequestMessage);

            var httpResponseBody = string.Empty;
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                httpResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();
            }

            Console.WriteLine(httpResponseBody);

            return new Response(httpResponseMessage.IsSuccessStatusCode, JsonConvert.DeserializeObject<dynamic>(httpResponseBody), httpResponseMessage.Headers, (int)httpResponseMessage.StatusCode);
        }
    }

    public class Response
    {
        public Response(bool Success, dynamic Data, HttpResponseHeaders Headers, int Code)
        {
            this.Success = Success;
            this.Data = Data;
            this.Headers = Headers;
            this.Code = Code;
        }

        public bool Success { get; init; }

        public dynamic Data { get; set; }

        public HttpResponseHeaders Headers { get; init; }

        public int Code { get; init; }
    }
}