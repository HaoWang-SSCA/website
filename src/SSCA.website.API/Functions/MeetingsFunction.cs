using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SSCA.website.API.Services;
using SSCA.website.Shared.Models;

namespace SSCA.website.API.Functions;

/// <summary>
/// Public API endpoints for meeting lists (no authentication required)
/// </summary>
public class MeetingsFunction
{
    private readonly IMeetingService _meetingService;

    public MeetingsFunction(IMeetingService meetingService)
    {
        _meetingService = meetingService;
    }

    [Function("GetSundayMessages")]
    public async Task<IActionResult> GetSundayMessages(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "meetings/sunday")] HttpRequest req)
    {
        var query = ParseQuery(req);
        var result = await _meetingService.GetSundayMessagesAsync(query);
        return new OkObjectResult(result);
    }

    [Function("GetGospelMeetings")]
    public async Task<IActionResult> GetGospelMeetings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "meetings/gospel")] HttpRequest req)
    {
        var query = ParseQuery(req);
        var result = await _meetingService.GetGospelMeetingsAsync(query);
        return new OkObjectResult(result);
    }

    [Function("GetSpecialMeetings")]
    public async Task<IActionResult> GetSpecialMeetings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "meetings/special")] HttpRequest req)
    {
        var query = ParseQuery(req);
        var result = await _meetingService.GetSpecialMeetingsAsync(query);
        return new OkObjectResult(result);
    }

    [Function("GetMeetingById")]
    public async Task<IActionResult> GetMeetingById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "meetings/{id:guid}")] HttpRequest req,
        Guid id)
    {
        var result = await _meetingService.GetByIdAsync(id);
        if (result == null)
            return new NotFoundResult();
        return new OkObjectResult(result);
    }

    [Function("GetSpeakers")]
    public async Task<IActionResult> GetSpeakers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "meetings/speakers")] HttpRequest req)
    {
        var type = req.Query["type"];
        var speakers = await _meetingService.GetDistinctSpeakersAsync(type);
        return new OkObjectResult(speakers);
    }

    private static MeetingSearchQuery ParseQuery(HttpRequest req)
    {
        var query = new MeetingSearchQuery();

        if (int.TryParse(req.Query["page"], out var page))
            query.Page = page;

        if (int.TryParse(req.Query["pageSize"], out var pageSize))
            query.PageSize = Math.Min(pageSize, 50); // Cap at 50

        query.Speaker = req.Query["speaker"];
        query.Topic = req.Query["topic"];

        if (DateTime.TryParse(req.Query["dateFrom"], out var dateFrom))
            query.DateFrom = dateFrom;

        if (DateTime.TryParse(req.Query["dateTo"], out var dateTo))
            query.DateTo = dateTo;

        return query;
    }
}
