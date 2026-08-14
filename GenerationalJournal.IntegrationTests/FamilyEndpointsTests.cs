using System.Net;
using System.Net.Http.Json;
using GenerationalJournal.Application.DTOs.Family;

namespace GenerationalJournal.IntegrationTests;

public class FamilyEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FamilyEndpointsTests(CustomWebApplicationFactory factory)
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

    [Fact]
    public async Task CreateFamily_WithAuth_ReturnsCreated()
    {
        var client = CreateClient();
        var auth = await client.RegisterAsync($"family-{Guid.NewGuid()}@example.com");
        client.Authenticate(auth.Token);

        var response = await client.PostAsJsonAsync("/api/families", new CreateFamilyRequest
        {
            Name = "Smith Family",
            Description = "Our family"
        }, TestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var family = await response.Content.ReadFromJsonAsync<FamilyResponse>(TestHelpers.JsonOptions);
        Assert.NotNull(family);
        Assert.Equal("Smith Family", family!.Name);
        Assert.Equal(auth.UserId, family.CreatedByUserId);
    }

    [Fact]
    public async Task GetFamilies_WithoutAuth_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/families");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFamilies_WithAuth_ReturnsFamilyList()
    {
        var client = CreateClient();
        var auth = await client.RegisterAsync($"families-{Guid.NewGuid()}@example.com");
        client.Authenticate(auth.Token);
        await CreateFamilyAsync(client);

        var response = await client.GetAsync("/api/families");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var families = await response.Content.ReadFromJsonAsync<List<FamilyResponse>>(TestHelpers.JsonOptions);
        Assert.NotNull(families);
        Assert.Single(families!);
    }

    [Fact]
    public async Task AddMember_ByEmail_ReturnsCreated()
    {
        var client = CreateClient();
        var auth = await client.RegisterAsync($"admin-{Guid.NewGuid()}@example.com");
        client.Authenticate(auth.Token);
        var family = await CreateFamilyAsync(client);

        var memberEmail = $"member-{Guid.NewGuid()}@example.com";
        var memberAuth = await CreateClient().RegisterAsync(memberEmail);

        var response = await client.PostAsJsonAsync($"/api/families/{family.Id}/members", new AddMemberRequest
        {
            Email = memberEmail,
            Role = "Member",
            RelationshipDescription = "Child"
        }, TestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var member = await response.Content.ReadFromJsonAsync<FamilyMemberResponse>(TestHelpers.JsonOptions);
        Assert.NotNull(member);
        Assert.Equal(memberAuth.UserId, member!.UserId);
        Assert.Equal(memberEmail, member.Email);
    }

    [Fact]
    public async Task GetFamilyMembers_ReturnsMembers()
    {
        var client = CreateClient();
        var auth = await client.RegisterAsync($"members-{Guid.NewGuid()}@example.com");
        client.Authenticate(auth.Token);
        var family = await CreateFamilyAsync(client);

        var response = await client.GetAsync($"/api/families/{family.Id}/members");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var members = await response.Content.ReadFromJsonAsync<List<FamilyMemberResponse>>(TestHelpers.JsonOptions);
        Assert.NotNull(members);
        Assert.Single(members!);
        Assert.Equal(auth.UserId, members![0].UserId);
        Assert.Equal("Admin", members[0].Role);
    }

    [Fact]
    public async Task RemoveMember_ReturnsNoContent()
    {
        var client = CreateClient();
        var adminAuth = await client.RegisterAsync($"remove-admin-{Guid.NewGuid()}@example.com");
        client.Authenticate(adminAuth.Token);
        var family = await CreateFamilyAsync(client);

        var memberEmail = $"remove-member-{Guid.NewGuid()}@example.com";
        var memberAuth = await CreateClient().RegisterAsync(memberEmail);

        await client.PostAsJsonAsync($"/api/families/{family.Id}/members", new AddMemberRequest
        {
            Email = memberEmail
        }, TestHelpers.JsonOptions);

        var removeResponse = await client.DeleteAsync($"/api/families/{family.Id}/members/{memberAuth.UserId}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var membersResponse = await client.GetAsync($"/api/families/{family.Id}/members");
        var members = await membersResponse.Content.ReadFromJsonAsync<List<FamilyMemberResponse>>(TestHelpers.JsonOptions);
        Assert.DoesNotContain(members!, m => m.UserId == memberAuth.UserId);
    }

    [Fact]
    public async Task CreateFamily_WithInvalidData_ReturnsBadRequest()
    {
        var client = CreateClient();
        var auth = await client.RegisterAsync($"badfamily-{Guid.NewGuid()}@example.com");
        client.Authenticate(auth.Token);

        var response = await client.PostAsJsonAsync("/api/families", new CreateFamilyRequest
        {
            Name = "",
            Description = ""
        }, TestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
