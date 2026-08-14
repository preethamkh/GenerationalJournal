namespace GenerationalJournal.Infrastructure.Repositories;

using GenerationalJournal.Domain.Entities;
using GenerationalJournal.Domain.Repositories;
using GenerationalJournal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class FamilyRepository : IFamilyRepository
{
    private readonly AppDbContext _context;

    public FamilyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Family?> GetByIdAsync(Guid id)
    {
        return await _context.Families.FindAsync(id);
    }

    public async Task<List<Family>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Families
            .Where(f => f.Members.Any(m => m.UserId == userId))
            .OrderBy(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<Family> CreateAsync(Family family)
    {
        _context.Families.Add(family);
        await _context.SaveChangesAsync();
        return family;
    }

    public async Task<Family> UpdateAsync(Family family)
    {
        _context.Families.Update(family);
        await _context.SaveChangesAsync();
        return family;
    }

    public async Task DeleteAsync(Family family)
    {
        _context.Families.Remove(family);
        await _context.SaveChangesAsync();
    }

    public async Task<FamilyMember> AddMemberAsync(FamilyMember member)
    {
        _context.FamilyMembers.Add(member);
        await _context.SaveChangesAsync();
        return member;
    }

    public async Task RemoveMemberAsync(Guid familyId, Guid userId)
    {
        var member = await _context.FamilyMembers
            .FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == userId);

        if (member is not null)
        {
            _context.FamilyMembers.Remove(member);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<FamilyMember>> GetMembersAsync(Guid familyId)
    {
        return await _context.FamilyMembers
            .Where(m => m.FamilyId == familyId)
            .Include(m => m.User)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync();
    }

    public async Task<FamilyMember?> GetMemberAsync(Guid familyId, Guid userId)
    {
        return await _context.FamilyMembers
            .FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == userId);
    }
}
