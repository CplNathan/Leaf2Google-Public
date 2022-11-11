using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Leaf2Google.Helpers;

public static class Request
{
    public static async Task<Response> MakeRequest<T>(this HttpClient client, HttpRequestMessage httpRequestMessage)
    {
        var httpResponseMessage = await client.SendAsync(httpRequestMessage);

        var httpResponseBody = string.Empty;
        if (httpResponseMessage.IsSuccessStatusCode)
            httpResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();

        Console.WriteLine(httpResponseBody);

        //JsonSerializer.Deserialize<T>(httpResponseBody);
        return new Response(httpResponseMessage.IsSuccessStatusCode,
            JsonConvert.DeserializeObject<dynamic>(httpResponseBody), httpResponseMessage.Headers,
            (int)httpResponseMessage.StatusCode);
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

    public bool Success { get; set; }

    public dynamic Data { get; set; }

    public HttpResponseHeaders Headers { get; init; }

    public int Code { get; init; }
}