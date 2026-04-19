using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SSCA.website.API.Services;
using SSCA.website.Shared.Models;

namespace SSCA.website.API.Functions;

/// <summary>
/// API endpoints for Friday study groups (周五查经小组)
/// </summary>
public class FridayGroupFunction
{
    private readonly IFridayGroupService _fridayGroupService;

    public FridayGroupFunction(IFridayGroupService fridayGroupService)
    {
        _fridayGroupService = fridayGroupService;
    }

    /// <summary>
    /// Get active Friday groups for public display
    /// </summary>
    [Function("GetActiveFridayGroups")]
    public async Task<IActionResult> GetActiveGroups(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "friday-groups")] HttpRequest req)
    {
        var groups = await _fridayGroupService.GetActiveGroupsAsync();
        return new OkObjectResult(groups);
    }

    /// <summary>
    /// Get all Friday groups for admin management
    /// </summary>
    [Function("AdminGetFridayGroups")]
    public async Task<IActionResult> GetAllGroups(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mgmt/friday-groups")] HttpRequest req)
    {
        var groups = await _fridayGroupService.GetAllGroupsAsync();
        return new OkObjectResult(groups);
    }

    /// <summary>
    /// Create a new Friday group
    /// </summary>
    [Function("AdminCreateFridayGroup")]
    public async Task<IActionResult> CreateFridayGroup(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mgmt/friday-groups")] HttpRequest req)
    {
        var request = await req.ReadFromJsonAsync<CreateFridayGroupRequest>();
        if (request == null)
            return new BadRequestObjectResult("Invalid request body");

        if (string.IsNullOrWhiteSpace(request.GroupName))
            return new BadRequestObjectResult("GroupName is required");

        if (string.IsNullOrWhiteSpace(request.BookName))
            return new BadRequestObjectResult("BookName is required");

        var result = await _fridayGroupService.CreateAsync(request);
        return new CreatedResult($"/api/mgmt/friday-groups/{result.Id}", result);
    }

    /// <summary>
    /// Update an existing Friday group
    /// </summary>
    [Function("AdminUpdateFridayGroup")]
    public async Task<IActionResult> UpdateFridayGroup(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "mgmt/friday-groups/{id:guid}")] HttpRequest req,
        Guid id)
    {
        var request = await req.ReadFromJsonAsync<UpdateFridayGroupRequest>();
        if (request == null)
            return new BadRequestObjectResult("Invalid request body");

        request.Id = id;
        var result = await _fridayGroupService.UpdateAsync(request);
        if (result == null)
            return new NotFoundResult();

        return new OkObjectResult(result);
    }

    /// <summary>
    /// Delete a Friday group
    /// </summary>
    [Function("AdminDeleteFridayGroup")]
    public async Task<IActionResult> DeleteFridayGroup(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "mgmt/friday-groups/{id:guid}")] HttpRequest req,
        Guid id)
    {
        var success = await _fridayGroupService.DeleteAsync(id);
        if (!success)
            return new NotFoundResult();

        return new NoContentResult();
    }
}
