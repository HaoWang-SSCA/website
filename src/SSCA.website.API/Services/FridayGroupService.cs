using Microsoft.EntityFrameworkCore;
using SSCA.website.API.Data;
using SSCA.website.API.Models;
using SSCA.website.Shared.Models;

namespace SSCA.website.API.Services;

public interface IFridayGroupService
{
    Task<List<FridayGroupDto>> GetActiveGroupsAsync();
    Task<List<FridayGroupDto>> GetAllGroupsAsync();
    Task<FridayGroupDto?> GetByIdAsync(Guid id);
    Task<FridayGroupDto> CreateAsync(CreateFridayGroupRequest request);
    Task<FridayGroupDto?> UpdateAsync(UpdateFridayGroupRequest request);
    Task<bool> DeleteAsync(Guid id);
}

public class FridayGroupService : IFridayGroupService
{
    private readonly AppDbContext _context;

    public FridayGroupService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<FridayGroupDto>> GetActiveGroupsAsync()
    {
        var groups = await _context.FridayGroups
            .Where(g => g.IsActive)
            .OrderBy(g => g.DisplayOrder)
            .ThenByDescending(g => g.CreatedAt)
            .ToListAsync();

        return groups.Select(ToDto).ToList();
    }

    public async Task<List<FridayGroupDto>> GetAllGroupsAsync()
    {
        var groups = await _context.FridayGroups
            .OrderBy(g => g.DisplayOrder)
            .ThenByDescending(g => g.CreatedAt)
            .ToListAsync();

        return groups.Select(ToDto).ToList();
    }

    public async Task<FridayGroupDto?> GetByIdAsync(Guid id)
    {
        var group = await _context.FridayGroups.FindAsync(id);
        return group == null ? null : ToDto(group);
    }

    public async Task<FridayGroupDto> CreateAsync(CreateFridayGroupRequest request)
    {
        var group = new FridayGroup
        {
            Id = Guid.NewGuid(),
            GroupName = request.GroupName,
            BookName = request.BookName,
            BookEnglishName = request.BookEnglishName,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.FridayGroups.Add(group);
        await _context.SaveChangesAsync();

        return ToDto(group);
    }

    public async Task<FridayGroupDto?> UpdateAsync(UpdateFridayGroupRequest request)
    {
        var group = await _context.FridayGroups.FindAsync(request.Id);
        if (group == null) return null;

        group.GroupName = request.GroupName;
        group.BookName = request.BookName;
        group.BookEnglishName = request.BookEnglishName;
        group.DisplayOrder = request.DisplayOrder;
        group.IsActive = request.IsActive;
        group.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ToDto(group);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var group = await _context.FridayGroups.FindAsync(id);
        if (group == null) return false;

        _context.FridayGroups.Remove(group);
        await _context.SaveChangesAsync();

        return true;
    }

    private static FridayGroupDto ToDto(FridayGroup group)
    {
        return new FridayGroupDto
        {
            Id = group.Id,
            GroupName = group.GroupName,
            BookName = group.BookName,
            BookEnglishName = group.BookEnglishName,
            DisplayOrder = group.DisplayOrder,
            IsActive = group.IsActive,
            CreatedAt = group.CreatedAt
        };
    }
}
