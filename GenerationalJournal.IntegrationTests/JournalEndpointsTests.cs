using System.Net;
using System.Net.Http.Json;
using GenerationalJournal.Application.DTOs.Family;
using GenerationalJournal.Application.DTOs.Journal;

namespace GenerationalJournal.IntegrationTests;

public class JournalEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public JournalEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private async Task<FamilyResponse> CreateFamilyAsync(HttpClient client, string name = "Smith Family")
    {
        var response = await client.PostAsJsonAsync("/api/families", new CreateFamilyRequest
        {
            Name = name,
            Description = "Our family"
        }, TestHelpers.JsonOptions);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FamilyResponse>(TestHelpers.JsonOptions))!;
    }

    private async Task<JournalEntryResponse> CreateEntryAsync(
        HttpClient client,
        Guid familyId,
        string title = "My Entry",
        string content = "Some content")
    {
        var response = await client.PostAsJsonAsync($"/api/families/{familyId}/entries", new CreateJournalEntryRequest
        {
            Title = title,
            Content = content,
            Mood = "Happy",
            IsPrivate = false,
            Tags = "vacation"
        }, TestHelpers.JsonOptions);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JournalEntryResponse>(TestHelpers.JsonOptions))!;
    }

    private async Task<(HttpClient Client, Guid FamilyId, Guid UserId)> SetupAdminWithFamilyAsync(string prefix)
    {
        var client = CreateClient();
        var auth = await client.RegisterAsync($"{prefix}-{Guid.NewGuid()}@example.com");
        client.Authenticate(auth.Token);
        var family = await CreateFamilyAsync(client);
        return (client, family.Id, auth.UserId);
    }

    [Fact]
    public async Task CreateEntry_WithAuth_ReturnsCreated()
    {
        var (client, familyId, userId) = await SetupAdminWithFamilyAsync("entry");

        var response = await client.PostAsJsonAsync($"/api/families/{familyId}/entries", new CreateJournalEntryRequest
        {
            Title = "First Entry",
            Content = "Hello family!",
            Mood = "Happy"
        }, TestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var entry = await response.Content.ReadFromJsonAsync<JournalEntryResponse>(TestHelpers.JsonOptions);
        Assert.NotNull(entry);
        Assert.Equal(userId, entry!.AuthorId);
        Assert.Equal(familyId, entry.FamilyId);
        Assert.Equal("First Entry", entry.Title);
    }

    [Fact]
    public async Task GetEntries_WithAuth_ReturnsList()
    {
        var (client, familyId, _) = await SetupAdminWithFamilyAsync("list");
        await CreateEntryAsync(client, familyId);

        var response = await client.GetAsync($"/api/families/{familyId}/entries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<JournalEntryListResponse>(TestHelpers.JsonOptions);
        Assert.NotNull(list);
        Assert.Equal(1, list!.TotalCount);
        Assert.Single(list.Items);
    }

    [Fact]
    public async Task GetEntryById_ReturnsEntry()
    {
        var (client, familyId, _) = await SetupAdminWithFamilyAsync("getbyid");
        var created = await CreateEntryAsync(client, familyId, "Read Me");

        var response = await client.GetAsync($"/api/entries/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var entry = await response.Content.ReadFromJsonAsync<JournalEntryResponse>(TestHelpers.JsonOptions);
        Assert.NotNull(entry);
        Assert.Equal("Read Me", entry!.Title);
    }

    [Fact]
    public async Task UpdateEntry_ByOwner_ReturnsUpdated()
    {
        var (client, familyId, _) = await SetupAdminWithFamilyAsync("update");
        var created = await CreateEntryAsync(client, familyId, "Original");

        var response = await client.PutAsJsonAsync($"/api/entries/{created.Id}", new UpdateJournalEntryRequest
        {
            Title = "Updated Title",
            Content = "Updated content",
            Mood = "Sad",
            IsPrivate = true,
            Tags = "updated"
        }, TestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var entry = await response.Content.ReadFromJsonAsync<JournalEntryResponse>(TestHelpers.JsonOptions);
        Assert.NotNull(entry);
        Assert.Equal("Updated Title", entry!.Title);
        Assert.True(entry.IsPrivate);
    }

    [Fact]
    public async Task UpdateEntry_ByNonOwner_ReturnsForbidden()
    {
        var (client, familyId, _) = await SetupAdminWithFamilyAsync("owner");

        var memberEmail = $"member-{Guid.NewGuid()}@example.com";
        var memberAuth = await CreateClient().RegisterAsync(memberEmail);

        await client.PostAsJsonAsync($"/api/families/{familyId}/members", new AddMemberRequest
        {
            Email = memberEmail
        }, TestHelpers.JsonOptions);

        var entry = await CreateEntryAsync(client, familyId, "Owner Entry");

        var memberClient = CreateClient();
        memberClient.Authenticate(memberAuth.Token);

        var response = await memberClient.PutAsJsonAsync($"/api/entries/{entry.Id}", new UpdateJournalEntryRequest
        {
            Title = "Hacked",
            Content = "Hacked content",
            Mood = "Angry"
        }, TestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEntry_ByOwner_ReturnsNoContent()
    {
        var (client, familyId, _) = await SetupAdminWithFamilyAsync("delete");
        var created = await CreateEntryAsync(client, familyId, "To Delete");

        var response = await client.DeleteAsync($"/api/entries/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync($"/api/entries/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task SearchEntries_ReturnsMatchingEntries()
    {
        var (client, familyId, _) = await SetupAdminWithFamilyAsync("search");
        await CreateEntryAsync(client, familyId, "Vacation Plans", "We are going to the beach");

        var response = await client.GetAsync("/api/entries/search?q=beach");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<JournalEntryListResponse>(TestHelpers.JsonOptions);
        Assert.NotNull(list);
        Assert.Equal(1, list!.TotalCount);
        Assert.Equal("Vacation Plans", list.Items[0].Title);
    }

    [Fact]
    public async Task GetEntries_WithoutAuth_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync($"/api/families/{Guid.NewGuid()}/entries");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
