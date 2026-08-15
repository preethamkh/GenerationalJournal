using System.Net.Http.Headers;
using System.Net.Http.Json;
using GenerationalJournal.Web.Models;

namespace GenerationalJournal.Web.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authStateProvider;

    public ApiClient(HttpClient http, AuthStateProvider authStateProvider)
    {
        _http = http;
        _authStateProvider = authStateProvider;
    }

    // Authentication

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        using var message = await CreateRequestAsync(HttpMethod.Post, "api/auth/login", request);
        using var response = await _http.SendAsync(message);
        return await HandleAsync<AuthResponse>(response);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        using var message = await CreateRequestAsync(HttpMethod.Post, "api/auth/register", request);
        using var response = await _http.SendAsync(message);
        return await HandleAsync<AuthResponse>(response);
    }

    // Families

    public async Task<List<FamilyResponse>> GetFamiliesAsync()
    {
        using var message = await CreateRequestAsync(HttpMethod.Get, "api/families");
        using var response = await _http.SendAsync(message);
        return await HandleAsync<List<FamilyResponse>>(response);
    }

    public async Task<FamilyResponse> GetFamilyAsync(Guid id)
    {
        using var message = await CreateRequestAsync(HttpMethod.Get, $"api/families/{id}");
        using var response = await _http.SendAsync(message);
        return await HandleAsync<FamilyResponse>(response);
    }

    public async Task<FamilyResponse> CreateFamilyAsync(CreateFamilyRequest request)
    {
        using var message = await CreateRequestAsync(HttpMethod.Post, "api/families", request);
        using var response = await _http.SendAsync(message);
        return await HandleAsync<FamilyResponse>(response);
    }

    public async Task<List<FamilyMemberResponse>> GetMembersAsync(Guid familyId)
    {
        using var message = await CreateRequestAsync(HttpMethod.Get, $"api/families/{familyId}/members");
        using var response = await _http.SendAsync(message);
        return await HandleAsync<List<FamilyMemberResponse>>(response);
    }

    public async Task<FamilyMemberResponse> AddMemberAsync(Guid familyId, AddMemberRequest request)
    {
        using var message = await CreateRequestAsync(HttpMethod.Post, $"api/families/{familyId}/members", request);
        using var response = await _http.SendAsync(message);
        return await HandleAsync<FamilyMemberResponse>(response);
    }

    public async Task RemoveMemberAsync(Guid familyId, Guid userId)
    {
        using var message = await CreateRequestAsync(HttpMethod.Delete, $"api/families/{familyId}/members/{userId}");
        using var response = await _http.SendAsync(message);
        await EnsureSuccessAsync(response);
    }

    // Journal entries

    public async Task<JournalEntryListResponse> GetEntriesAsync(Guid familyId, int page = 1, int pageSize = 20)
    {
        using var message = await CreateRequestAsync(
            HttpMethod.Get, $"api/families/{familyId}/entries?page={page}&pageSize={pageSize}");
        using var response = await _http.SendAsync(message);
        return await HandleAsync<JournalEntryListResponse>(response);
    }

    public async Task<JournalEntryResponse> GetEntryAsync(Guid id)
    {
        using var message = await CreateRequestAsync(HttpMethod.Get, $"api/entries/{id}");
        using var response = await _http.SendAsync(message);
        return await HandleAsync<JournalEntryResponse>(response);
    }

    public async Task<JournalEntryResponse> CreateEntryAsync(Guid familyId, CreateJournalEntryRequest request)
    {
        using var message = await CreateRequestAsync(HttpMethod.Post, $"api/families/{familyId}/entries", request);
        using var response = await _http.SendAsync(message);
        return await HandleAsync<JournalEntryResponse>(response);
    }

    public async Task<JournalEntryResponse> UpdateEntryAsync(Guid id, UpdateJournalEntryRequest request)
    {
        using var message = await CreateRequestAsync(HttpMethod.Put, $"api/entries/{id}", request);
        using var response = await _http.SendAsync(message);
        return await HandleAsync<JournalEntryResponse>(response);
    }

    public async Task DeleteEntryAsync(Guid id)
    {
        using var message = await CreateRequestAsync(HttpMethod.Delete, $"api/entries/{id}");
        using var response = await _http.SendAsync(message);
        await EnsureSuccessAsync(response);
    }

    // Media

    public async Task<List<MediaResponse>> GetMediaAsync(Guid entryId)
    {
        using var message = await CreateRequestAsync(HttpMethod.Get, $"api/entries/{entryId}/media");
        using var response = await _http.SendAsync(message);
        return await HandleAsync<List<MediaResponse>>(response);
    }

    public async Task<MediaResponse> UploadMediaAsync(Guid entryId, string fileName, Stream content, string contentType)
    {
        using var message = await CreateRequestAsync(HttpMethod.Post, $"api/entries/{entryId}/media");
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        form.Add(fileContent, "file", fileName);
        message.Content = form;
        using var response = await _http.SendAsync(message);
        return await HandleAsync<MediaResponse>(response);
    }

    public async Task DeleteMediaAsync(Guid mediaId)
    {
        using var message = await CreateRequestAsync(HttpMethod.Delete, $"api/media/{mediaId}");
        using var response = await _http.SendAsync(message);
        await EnsureSuccessAsync(response);
    }

    public async Task<byte[]> GetMediaFileAsync(Guid mediaId)
    {
        using var message = await CreateRequestAsync(HttpMethod.Get, $"api/media/{mediaId}/file");
        using var response = await _http.SendAsync(message);
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiException.CreateAsync(response);
        }

        return await response.Content.ReadAsByteArrayAsync();
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url, object? body = null)
    {
        var message = new HttpRequestMessage(method, url);

        var token = await _authStateProvider.GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            message.Content = JsonContent.Create(body);
        }

        return message;
    }

    private static async Task<T> HandleAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<T>();
            return result ?? throw new ApiException("The API returned an empty response.", response.StatusCode);
        }

        throw await ApiException.CreateAsync(response);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await ApiException.CreateAsync(response);
        }
    }
}
