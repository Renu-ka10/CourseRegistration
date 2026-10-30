using CourseWaitlist.Api.Models;
using CourseWaitlist.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourseWaitlist.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CourseController : ControllerBase
{
    private readonly ICourseRegistrationService _registrationService;

    // Static sample course for testing/demonstration
    private static readonly CourseOffering SampleCourse = new()
    {
        CourseId = "CS101",
        Title = "Introduction to Computer Science",
        Capacity = 2,
        PrerequisiteCourseIds = new List<string> { "MATH101" }
    };

    // In-memory learner database simulation
    private static readonly Dictionary<string, Learner> LearnersDb = new()
    {
        { "L1", new Learner { LearnerId = "L1", Name = "Alice", CompletedCourseIds = new() { "MATH101" } } },
        { "L2", new Learner { LearnerId = "L2", Name = "Bob", CompletedCourseIds = new() { "MATH101" } } },
        { "L3", new Learner { LearnerId = "L3", Name = "Charlie", CompletedCourseIds = new() { "MATH101" } } },
        { "L4", new Learner { LearnerId = "L4", Name = "Diana", CompletedCourseIds = new() } } // Missing prereq
    };

    public CourseController(ICourseRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    [HttpPost("enroll")]
    public async Task<IActionResult> Enroll([FromBody] EnrollRequest request)
    {
        if (!LearnersDb.TryGetValue(request.LearnerId, out var learner))
        {
            return NotFound(new { message = "Learner not found." });
        }

        var (success, message, status) = await _registrationService.EnrollAsync(SampleCourse, learner);

        if (!success)
        {
            return BadRequest(new { message, status });
        }

        return Ok(new { message, status });
    }

    [HttpPost("drop")]
    public async Task<IActionResult> Drop([FromBody] DropRequest request)
    {
        var (success, message) = await _registrationService.DropAsync(SampleCourse, request.LearnerId);
        if (!success)
        {
            return NotFound(new { message });
        }

        return Ok(new { message });
    }

    [HttpGet("state")]
    public async Task<IActionResult> GetState()
    {
        var state = await _registrationService.GetStateAsync();
        return Ok(state);
    }
}

public record EnrollRequest(string LearnerId);
public record DropRequest(string LearnerId);