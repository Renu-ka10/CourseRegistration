using CourseWaitlist.Api.Models;

namespace CourseWaitlist.Api.Services;

public interface ICourseRegistrationService
{
    Task<(bool Success, string Message, string Status)> EnrollAsync(CourseOffering course, Learner learner);
    Task<(bool Success, string Message)> DropAsync(CourseOffering course, string learnerId);
    Task<RegistrationState> GetStateAsync();
}