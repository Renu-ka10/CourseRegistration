namespace CourseWaitlist.Api.Models;

public class WaitlistItem
{
    public string LearnerId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class RegistrationState
{
    public HashSet<string> EnrolledLearnerIds { get; set; } = new(StringComparer.Ordinal);
    public List<WaitlistItem> Waitlist { get; set; } = new();
}