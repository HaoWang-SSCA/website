using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SSCA.website.API.Data;
using SSCA.website.API.Models;
using SSCA.website.Shared.Models;
using System.Text.RegularExpressions;

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
    Task<List<string>> GetDistinctSpeakersAsync(string? type = null);
}

public class MeetingService : IMeetingService
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly string _storageBaseUrl;
    private const string SpeakersCacheKey = "DistinctSpeakers";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public MeetingService(AppDbContext context, IConfiguration configuration, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
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
            Scripture = request.Scripture,
            VideoUrl = request.VideoUrl,
            IsGospel = request.IsGospel,
            IsSpecialMeeting = request.IsSpecialMeeting,
            AudioBlobName = request.AudioBlobName,
            PowerPointBlobName = request.PowerPointBlobName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MessageMeetings.Add(meeting);
        await _context.SaveChangesAsync();
        InvalidateSpeakersCache();
        return ToDto(meeting);
    }

    public async Task<MessageMeetingDto?> UpdateAsync(UpdateMeetingRequest request)
    {
        var meeting = await _context.MessageMeetings.FindAsync(request.Id);
        if (meeting == null) return null;

        meeting.Date = DateTime.SpecifyKind(request.Date, DateTimeKind.Utc);
        meeting.Speaker = request.Speaker;
        meeting.Topic = request.Topic;
        meeting.Scripture = request.Scripture;
        meeting.VideoUrl = request.VideoUrl;
        meeting.IsGospel = request.IsGospel;
        meeting.IsSpecialMeeting = request.IsSpecialMeeting;
        if (!string.IsNullOrEmpty(request.AudioBlobName))
            meeting.AudioBlobName = request.AudioBlobName;
        if (!string.IsNullOrEmpty(request.PowerPointBlobName))
            meeting.PowerPointBlobName = request.PowerPointBlobName;
        meeting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        InvalidateSpeakersCache();
        return ToDto(meeting);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var meeting = await _context.MessageMeetings.FindAsync(id);
        if (meeting == null) return false;

        _context.MessageMeetings.Remove(meeting);
        await _context.SaveChangesAsync();
        InvalidateSpeakersCache();
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

    public async Task<List<string>> GetDistinctSpeakersAsync(string? type = null)
    {
        type = type?.ToLowerInvariant();
        string cacheKey = string.IsNullOrEmpty(type) ? SpeakersCacheKey : $"{SpeakersCacheKey}_{type}";

        if (_cache.TryGetValue(cacheKey, out List<string>? cachedSpeakers) && cachedSpeakers != null)
        {
            return cachedSpeakers;
        }

        var query = _context.MessageMeetings.AsQueryable();

        if (!string.IsNullOrEmpty(type))
        {
            query = type.ToLower() switch
            {
                "sunday" => query.Where(m => !m.IsGospel && !m.IsSpecialMeeting),
                "gospel" => query.Where(m => m.IsGospel),
                "special" => query.Where(m => m.IsSpecialMeeting),
                _ => query
            };
        }

        var rawSpeakers = await query
            .Select(m => m.Speaker)
            .Distinct()
            .ToListAsync();

        var speakers = rawSpeakers
            .Select(CleanSpeakerName)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(GetSpeakerSortKey)
            .ThenBy(s => s)
            .ToList();

        _cache.Set(cacheKey, speakers, CacheDuration);
        return speakers;
    }

    private void InvalidateSpeakersCache()
    {
        _cache.Remove(SpeakersCacheKey);
        _cache.Remove($"{SpeakersCacheKey}_sunday");
        _cache.Remove($"{SpeakersCacheKey}_gospel");
        _cache.Remove($"{SpeakersCacheKey}_special");
    }

    private async Task<PagedResult<MessageMeetingDto>> GetPagedResultAsync(
        IQueryable<MessageMeeting> query, MeetingSearchQuery searchQuery)
    {
        // Apply search filters
        if (!string.IsNullOrWhiteSpace(searchQuery.Speaker))
        {
            var speaker = searchQuery.Speaker.Trim();
            var cleanedSpeaker = CleanSpeakerName(speaker);
            var compactSpeaker = cleanedSpeaker.Replace(" ", "");
            query = query.Where(m =>
                m.Speaker.Contains(speaker) ||
                m.Speaker.Contains(cleanedSpeaker) ||
                m.Speaker.Contains(compactSpeaker) ||
                m.Speaker.Contains(cleanedSpeaker + "弟兄") ||
                m.Speaker.Contains(cleanedSpeaker + "姐妹") ||
                m.Speaker.Contains(compactSpeaker + "弟兄") ||
                m.Speaker.Contains(compactSpeaker + "姐妹") ||
                m.Speaker.Contains("Bro. " + cleanedSpeaker) ||
                m.Speaker.Contains("Brother " + cleanedSpeaker) ||
                m.Speaker.Contains("Sis. " + cleanedSpeaker) ||
                m.Speaker.Contains("Sister " + cleanedSpeaker));
        }

        if (!string.IsNullOrWhiteSpace(searchQuery.Topic))
            query = query.Where(m => m.Topic.Contains(searchQuery.Topic));

        if (!string.IsNullOrWhiteSpace(searchQuery.Scripture))
            query = query.Where(m => m.Scripture != null && m.Scripture.Contains(searchQuery.Scripture));

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
            Speaker = CleanSpeakerName(meeting.Speaker),
            Topic = meeting.Topic,
            Scripture = meeting.Scripture,
            AudioUrl = string.IsNullOrEmpty(meeting.AudioBlobName)
                ? null
                : $"{_storageBaseUrl}/audio-files/{meeting.AudioBlobName}",
            PowerPointUrl = string.IsNullOrEmpty(meeting.PowerPointBlobName)
                ? null
                : $"{_storageBaseUrl}/powerpoint-files/{meeting.PowerPointBlobName}",
            VideoUrl = meeting.VideoUrl,
            IsGospel = meeting.IsGospel,
            IsSpecialMeeting = meeting.IsSpecialMeeting,
            CreatedAt = meeting.CreatedAt,
            UpdatedAt = meeting.UpdatedAt
        };
    }

    private static string CleanSpeakerName(string? speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker)) return string.Empty;

        var cleaned = speaker.Trim();
        cleaned = Regex.Replace(cleaned, @"\s+", " ");
        cleaned = Regex.Replace(cleaned, @"\b(Bro|Brother|Sis|Sister)\.?\b", "", RegexOptions.IgnoreCase);
        cleaned = cleaned.Replace("弟兄", "").Replace("姐妹", "").Trim();
        cleaned = Regex.Replace(cleaned, @"\s+", " ");
        return cleaned;
    }

    private static string GetSpeakerSortKey(string speaker)
    {
        var cleaned = CleanSpeakerName(speaker);
        var latinMatch = Regex.Match(cleaned, "[A-Za-z]");
        if (latinMatch.Success)
        {
            var latinName = Regex.Replace(cleaned, "[^A-Za-z ]", "").Trim();
            return $"0-{latinName.ToUpperInvariant()}";
        }

        return $"1-{GetPinyinSortKey(cleaned)}";
    }

    private static string GetPinyinSortKey(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        var knownNames = new Dictionary<string, string>
        {
            ["王"] = "Wang", ["李"] = "Li", ["张"] = "Zhang", ["刘"] = "Liu", ["陈"] = "Chen",
            ["杨"] = "Yang", ["黄"] = "Huang", ["赵"] = "Zhao", ["吴"] = "Wu", ["周"] = "Zhou",
            ["徐"] = "Xu", ["孙"] = "Sun", ["马"] = "Ma", ["朱"] = "Zhu", ["胡"] = "Hu",
            ["郭"] = "Guo", ["何"] = "He", ["林"] = "Lin", ["高"] = "Gao", ["罗"] = "Luo",
            ["郑"] = "Zheng", ["梁"] = "Liang", ["谢"] = "Xie", ["宋"] = "Song", ["唐"] = "Tang",
            ["许"] = "Xu", ["邓"] = "Deng", ["冯"] = "Feng", ["韩"] = "Han", ["曹"] = "Cao",
            ["彭"] = "Peng", ["曾"] = "Zeng", ["萧"] = "Xiao", ["肖"] = "Xiao", ["田"] = "Tian",
            ["董"] = "Dong", ["袁"] = "Yuan", ["潘"] = "Pan", ["蔡"] = "Cai", ["蒋"] = "Jiang",
            ["余"] = "Yu", ["杜"] = "Du", ["叶"] = "Ye", ["程"] = "Cheng", ["苏"] = "Su",
            ["魏"] = "Wei", ["吕"] = "Lu", ["丁"] = "Ding", ["沈"] = "Shen", ["任"] = "Ren",
            ["姚"] = "Yao", ["卢"] = "Lu", ["姜"] = "Jiang", ["崔"] = "Cui", ["钟"] = "Zhong",
            ["谭"] = "Tan", ["陆"] = "Lu", ["汪"] = "Wang", ["范"] = "Fan", ["金"] = "Jin",
            ["石"] = "Shi", ["戴"] = "Dai", ["贾"] = "Jia", ["邱"] = "Qiu", ["方"] = "Fang"
        };

        foreach (var item in knownNames.OrderByDescending(k => k.Key.Length))
        {
            if (name.StartsWith(item.Key, StringComparison.Ordinal))
                return item.Value + name;
        }

        return name;
    }

}
