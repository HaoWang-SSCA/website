using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SSCA.website.API.Data;
using SSCA.website.API.Models;
using SSCA.website.Shared.Models;

namespace SSCA.website.API.Services;

public interface IMeetingService
{
    Task<PagedResult<MessageMeetingDto>> GetSundayMessagesAsync(MeetingSearchQuery query);
    Task<PagedResult<MessageMeetingDto>> GetGospelMeetingsAsync(MeetingSearchQuery query);
    Task<PagedResult<MessageMeetingDto>> GetSpecialMeetingsAsync(MeetingSearchQuery query);
    Task<MessageMeetingDto?> GetByIdAsync(Guid id);
    Task<PagedResult<MessageMeetingDto>> GetAllAsync(MeetingSearchQuery query);
    Task<MessageMeetingDto> CreateAsync(CreateMeetingRequest request);
    Task<MessageMeetingDto?> UpdateAsync(UpdateMeetingRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> UpdateAudioBlobAsync(Guid id, string blobName);
}

public class MeetingService : IMeetingService
{
    private readonly AppDbContext _context;
    private readonly string _storageBaseUrl;

    public MeetingService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _storageBaseUrl = configuration["AzureStorage:BaseUrl"] ?? "";
    }

    public async Task<PagedResult<MessageMeetingDto>> GetSundayMessagesAsync(MeetingSearchQuery query)
    {
        var baseQuery = _context.MessageMeetings
            .Where(m => !m.IsGospel && !m.IsSpecialMeeting);
        return await GetPagedResultAsync(baseQuery, query);
    }

    public async Task<PagedResult<MessageMeetingDto>> GetGospelMeetingsAsync(MeetingSearchQuery query)
    {
        var baseQuery = _context.MessageMeetings.Where(m => m.IsGospel);
        return await GetPagedResultAsync(baseQuery, query);
    }

    public async Task<PagedResult<MessageMeetingDto>> GetSpecialMeetingsAsync(MeetingSearchQuery query)
    {
        var baseQuery = _context.MessageMeetings.Where(m => m.IsSpecialMeeting);
        return await GetPagedResultAsync(baseQuery, query);
    }

    public async Task<MessageMeetingDto?> GetByIdAsync(Guid id)
    {
        var meeting = await _context.MessageMeetings.FindAsync(id);
        return meeting == null ? null : ToDto(meeting);
    }

    public async Task<PagedResult<MessageMeetingDto>> GetAllAsync(MeetingSearchQuery query)
    {
        var baseQuery = _context.MessageMeetings.AsQueryable();
        return await GetPagedResultAsync(baseQuery, query);
    }

    public async Task<MessageMeetingDto> CreateAsync(CreateMeetingRequest request)
    {
        var meeting = new MessageMeeting
        {
            Id = Guid.NewGuid(),
            Date = DateTime.SpecifyKind(request.Date, DateTimeKind.Utc),
            Speaker = request.Speaker,
            Topic = request.Topic,
            VideoUrl = request.VideoUrl,
            IsGospel = request.IsGospel,
            IsSpecialMeeting = request.IsSpecialMeeting,
            AudioBlobName = request.AudioBlobName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MessageMeetings.Add(meeting);
        await _context.SaveChangesAsync();
        return ToDto(meeting);
    }

    public async Task<MessageMeetingDto?> UpdateAsync(UpdateMeetingRequest request)
    {
        var meeting = await _context.MessageMeetings.FindAsync(request.Id);
        if (meeting == null) return null;

        meeting.Date = DateTime.SpecifyKind(request.Date, DateTimeKind.Utc);
        meeting.Speaker = request.Speaker;
        meeting.Topic = request.Topic;
        meeting.VideoUrl = request.VideoUrl;
        meeting.IsGospel = request.IsGospel;
        meeting.IsSpecialMeeting = request.IsSpecialMeeting;
        if (!string.IsNullOrEmpty(request.AudioBlobName))
            meeting.AudioBlobName = request.AudioBlobName;
        meeting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return ToDto(meeting);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var meeting = await _context.MessageMeetings.FindAsync(id);
        if (meeting == null) return false;

        _context.MessageMeetings.Remove(meeting);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateAudioBlobAsync(Guid id, string blobName)
    {
        var meeting = await _context.MessageMeetings.FindAsync(id);
        if (meeting == null) return false;

        meeting.AudioBlobName = blobName;
        meeting.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<PagedResult<MessageMeetingDto>> GetPagedResultAsync(
        IQueryable<MessageMeeting> query, MeetingSearchQuery searchQuery)
    {
        // Apply search filters
        if (!string.IsNullOrWhiteSpace(searchQuery.Speaker))
            query = query.Where(m => m.Speaker.Contains(searchQuery.Speaker));

        if (!string.IsNullOrWhiteSpace(searchQuery.Topic))
            query = query.Where(m => m.Topic.Contains(searchQuery.Topic));

        if (searchQuery.DateFrom.HasValue)
            query = query.Where(m => m.Date >= searchQuery.DateFrom.Value);

        if (searchQuery.DateTo.HasValue)
            query = query.Where(m => m.Date <= searchQuery.DateTo.Value);

        var totalCount = await query.CountAsync();

        var entities = await query
            .OrderByDescending(m => m.Date)
            .Skip((searchQuery.Page - 1) * searchQuery.PageSize)
            .Take(searchQuery.PageSize)
            .ToListAsync();

        var items = entities.Select(m => ToDto(m)).ToList();

        return new PagedResult<MessageMeetingDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = searchQuery.Page,
            PageSize = searchQuery.PageSize
        };
    }

    private MessageMeetingDto ToDto(MessageMeeting meeting)
    {
        return new MessageMeetingDto
        {
            Id = meeting.Id,
            Date = meeting.Date,
            Speaker = meeting.Speaker,
            Topic = meeting.Topic,
            AudioUrl = string.IsNullOrEmpty(meeting.AudioBlobName) 
                ? null 
                : $"{_storageBaseUrl}/audio-files/{meeting.AudioBlobName}",
            VideoUrl = meeting.VideoUrl,
            IsGospel = meeting.IsGospel,
            IsSpecialMeeting = meeting.IsSpecialMeeting,
            CreatedAt = meeting.CreatedAt,
            UpdatedAt = meeting.UpdatedAt
        };
    }
}
