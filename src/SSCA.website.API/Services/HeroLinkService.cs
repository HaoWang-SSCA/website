using Microsoft.EntityFrameworkCore;
using SSCA.website.API.Data;
using SSCA.website.API.Models;
using SSCA.website.Shared.Models;

namespace SSCA.website.API.Services;

public interface IHeroLinkService
{
    Task<List<HeroLinkDto>> GetActiveLinksAsync();
    Task<List<HeroLinkDto>> GetAllLinksAsync();
    Task<HeroLinkDto?> GetByIdAsync(Guid id);
    Task<HeroLinkDto> CreateAsync(CreateHeroLinkRequest request);
    Task<HeroLinkDto?> UpdateAsync(UpdateHeroLinkRequest request);
    Task<bool> DeleteAsync(Guid id);
}

public class HeroLinkService : IHeroLinkService
{
    private readonly AppDbContext _context;

    public HeroLinkService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get only active (non-expired) links for public display
    /// </summary>
    public async Task<List<HeroLinkDto>> GetActiveLinksAsync()
    {
        var now = DateTime.UtcNow;
        
        var links = await _context.HeroLinks
            .Where(h => h.ExpiryDate > now)
            .OrderBy(h => h.DisplayOrder)
            .ThenByDescending(h => h.CreatedAt)
            .ToListAsync();

        return links.Select(ToDto).ToList();
    }

    /// <summary>
    /// Get all links for admin management (including expired)
    /// </summary>
    public async Task<List<HeroLinkDto>> GetAllLinksAsync()
    {
        var links = await _context.HeroLinks
            .OrderBy(h => h.DisplayOrder)
            .ThenByDescending(h => h.CreatedAt)
            .ToListAsync();

        return links.Select(ToDto).ToList();
    }

    public async Task<HeroLinkDto?> GetByIdAsync(Guid id)
    {
        var link = await _context.HeroLinks.FindAsync(id);
        return link == null ? null : ToDto(link);
    }

    public async Task<HeroLinkDto> CreateAsync(CreateHeroLinkRequest request)
    {
        var link = new HeroLink
        {
            Id = Guid.NewGuid(),
            Text = request.Text,
            ExternalUrl = request.ExternalUrl,
            FileBlobName = request.FileBlobName,
            ExpiryDate = request.ExpiryDate,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.HeroLinks.Add(link);
        await _context.SaveChangesAsync();

        return ToDto(link);
    }

    public async Task<HeroLinkDto?> UpdateAsync(UpdateHeroLinkRequest request)
    {
        var link = await _context.HeroLinks.FindAsync(request.Id);
        if (link == null) return null;

        link.Text = request.Text;
        link.ExternalUrl = request.ExternalUrl;
        link.FileBlobName = request.FileBlobName;
        link.ExpiryDate = request.ExpiryDate;
        link.DisplayOrder = request.DisplayOrder;
        link.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ToDto(link);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var link = await _context.HeroLinks.FindAsync(id);
        if (link == null) return false;

        _context.HeroLinks.Remove(link);
        await _context.SaveChangesAsync();

        return true;
    }

    private static HeroLinkDto ToDto(HeroLink link)
    {
        return new HeroLinkDto
        {
            Id = link.Id,
            Text = link.Text,
            ExternalUrl = link.ExternalUrl,
            FileBlobName = link.FileBlobName,
            ExpiryDate = link.ExpiryDate,
            DisplayOrder = link.DisplayOrder,
            IsActive = link.ExpiryDate > DateTime.UtcNow,
            CreatedAt = link.CreatedAt
        };
    }
}
