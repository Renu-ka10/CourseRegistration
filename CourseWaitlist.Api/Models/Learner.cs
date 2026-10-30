namespace CourseWaitlist.Api.Models;

public class Learner
{
    public string LearnerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> CompletedCourseIds { get; set; } = new();
}