namespace GenerationalJournal.Application.Services;

using GenerationalJournal.Application.DTOs.Family;

public interface IFamilyService
{
    Task<FamilyResponse> CreateFamilyAsync(CreateFamilyRequest request, Guid userId);
    Task<List<FamilyResponse>> GetUserFamiliesAsync(Guid userId);
    Task<FamilyResponse> GetFamilyByIdAsync(Guid familyId, Guid userId);
    Task<FamilyMemberResponse> AddMemberAsync(Guid familyId, AddMemberRequest request, Guid requesterUserId);
    Task<List<FamilyMemberResponse>> GetFamilyMembersAsync(Guid familyId, Guid userId);
    Task RemoveMemberAsync(Guid familyId, Guid memberUserId, Guid requesterUserId);
}
