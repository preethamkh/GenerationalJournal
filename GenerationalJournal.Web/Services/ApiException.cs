using System.Net;
using System.Text.Json;

namespace GenerationalJournal.Web.Services;

public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public ApiException(string message, HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public static async Task<ApiException> CreateAsync(HttpResponseMessage response)
    {
        var message = $"The request failed with status code {(int)response.StatusCode} ({response.StatusCode}).";

        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = document.RootElement;

            if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
            {
                message = error.GetString() ?? message;
            }
            else if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
            {
                var items = errors.EnumerateArray()
                    .Select(e => e.GetString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                if (items.Count > 0)
                {
                    message = string.Join(" ", items);
                }
            }
        }
        catch
        {
            // Fall back to the generic message when the response body cannot be parsed.
        }

        return new ApiException(message, response.StatusCode);
    }
}
