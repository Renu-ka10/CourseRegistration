namespace CourseWaitlist.Api.Models;

public class CourseOffering
{
    public string CourseId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public List<string> PrerequisiteCourseIds { get; set; } = new();
}