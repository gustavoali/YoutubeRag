using Microsoft.Playwright;
using System.Text;
using System.Text.Json;

namespace YoutubeRag.Tests.E2E.PageObjects;

/// <summary>
/// Base API client for making HTTP requests using Playwright's APIRequestContext
/// </summary>
public class ApiClient
{
    protected readonly IAPIRequestContext RequestContext;
    protected readonly string BaseUrl;
    protected string? AuthToken;

    public ApiClient(IAPIRequestContext requestContext, string baseUrl)
    {
        RequestContext = requestContext;
        BaseUrl = baseUrl;
    }

    public void SetAuthToken(string token)
    {
        AuthToken = token;
    }

    protected Dictionary<string, string> GetHeaders(Dictionary<string, string>? additionalHeaders = null)
    {
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json"
        };

        if (!string.IsNullOrEmpty(AuthToken))
        {
            headers["Authorization"] = $"Bearer {AuthToken}";
        }

        if (additionalHeaders != null)
        {
            foreach (var header in additionalHeaders)
            {
                headers[header.Key] = header.Value;
            }
        }

        return headers;
    }

    protected async Task<IAPIResponse> GetAsync(string endpoint, Dictionary<string, string>? headers = null)
    {
        var url = $"{BaseUrl}{endpoint}";
        return await RequestContext.GetAsync(url, new APIRequestContextOptions
        {
            Headers = GetHeaders(headers)
        });
    }

    protected async Task<IAPIResponse> PostAsync(string endpoint, object? data = null, Dictionary<string, string>? headers = null)
    {
        var url = $"{BaseUrl}{endpoint}";
        var jsonData = data != null ? JsonSerializer.Serialize(data) : "{}";

        return await RequestContext.PostAsync(url, new APIRequestContextOptions
        {
            Headers = GetHeaders(headers),
            Data = jsonData
        });
    }

    protected async Task<IAPIResponse> PutAsync(string endpoint, object? data = null, Dictionary<string, string>? headers = null)
    {
        var url = $"{BaseUrl}{endpoint}";
        var jsonData = data != null ? JsonSerializer.Serialize(data) : "{}";

        return await RequestContext.PutAsync(url, new APIRequestContextOptions
        {
            Headers = GetHeaders(headers),
            Data = jsonData
        });
    }

    protected async Task<IAPIResponse> DeleteAsync(string endpoint, Dictionary<string, string>? headers = null)
    {
        var url = $"{BaseUrl}{endpoint}";
        return await RequestContext.DeleteAsync(url, new APIRequestContextOptions
        {
            Headers = GetHeaders(headers)
        });
    }

    protected async Task<T?> DeserializeResponse<T>(IAPIResponse response)
    {
        var responseBody = await response.TextAsync();
        if (string.IsNullOrEmpty(responseBody))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
