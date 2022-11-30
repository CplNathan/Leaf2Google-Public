﻿using System.Net.Http.Headers;
using System.Text.Json;

namespace Leaf2Google.Helpers;

public static class Request
{
    public static async Task<Response<T>?> MakeRequest<T>(this HttpClient client, HttpRequestMessage httpRequestMessage)
    {
        var httpResponseMessage = await client.SendAsync(httpRequestMessage);

        var httpResponseBody = string.Empty;
        if (httpResponseMessage.IsSuccessStatusCode)
            httpResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();

        Console.WriteLine(httpResponseBody);

        T? jsonResult = default(T);

        try
        {
            jsonResult = JsonSerializer.Deserialize<T>(httpResponseBody);
        }
        catch (JsonException ex)
        {
            // Parsing exceptions are okay to ignore.
            Console.WriteLine(ex.Message);
        }

        return new Response<T>(httpResponseMessage.IsSuccessStatusCode,
            jsonResult!, httpResponseMessage.Headers,
            (int)httpResponseMessage.StatusCode);
    }
}

public class Response<T>
{
    public Response(bool Success, T Data, HttpResponseHeaders Headers, int Code)
    {
        this.Success = Success;
        this.Data = Data;
        this.Headers = Headers;
        this.Code = Code;
    }

    public bool Success { get; set; }

    public T Data { get; set; }

    public HttpResponseHeaders Headers { get; init; }

    public int Code { get; init; }
}

[Obsolete]
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