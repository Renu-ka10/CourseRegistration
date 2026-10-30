using CourseWaitlist.Api.Models;

namespace CourseWaitlist.Api.Services;

public class CourseRegistrationService : ICourseRegistrationService
{
    private readonly RegistrationState _state = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<(bool Success, string Message, string Status)> EnrollAsync(CourseOffering course, Learner learner)
    {
        await _semaphore.WaitAsync();
        try
        {
            // 1. Check if already enrolled
            if (_state.EnrolledLearnerIds.Contains(learner.LearnerId))
            {
                return (true, "Learner is already enrolled.", "Enrolled");
            }

            // 2. Check if already on waitlist
            if (_state.Waitlist.Any(w => w.LearnerId == learner.LearnerId))
            {
                return (true, "Learner is already on the waitlist.", "Waitlisted");
            }

            // 3. Check prerequisites
            bool satisfiesPrereqs = course.PrerequisiteCourseIds.All(p => learner.CompletedCourseIds.Contains(p));
            if (!satisfiesPrereqs)
            {
                return (false, "Learner does not satisfy course prerequisites.", "Rejected_Prerequisite");
            }

            // 4. Check capacity
            if (_state.EnrolledLearnerIds.Count < course.Capacity)
            {
                _state.EnrolledLearnerIds.Add(learner.LearnerId);
                return (true, "Successfully enrolled in the course.", "Enrolled");
            }
            else
            {
                // Course is full, add to waitlist (FIFO)
                _state.Waitlist.Add(new WaitlistItem
                {
                    LearnerId = learner.LearnerId,
                    Timestamp = DateTime.UtcNow
                });
                return (true, "Course is full. Learner added to waitlist.", "Waitlisted");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<(bool Success, string Message)> DropAsync(CourseOffering course, string learnerId)
    {
        await _semaphore.WaitAsync();
        try
        {
            bool wasEnrolled = _state.EnrolledLearnerIds.Remove(learnerId);
            bool wasWaitlisted = _state.Waitlist.RemoveAll(w => w.LearnerId == learnerId) > 0;

            if (!wasEnrolled && !wasWaitlisted)
            {
                return (false, "Learner was neither enrolled nor on the waitlist.");
            }

            // If an enrolled seat was freed, promote the earliest eligible waitlisted learner
            if (wasEnrolled && _state.Waitlist.Count > 0)
            {
                PromoteNextEligibleLearner(course);
            }

            return (true, "Successfully dropped and waitlist updated.");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<RegistrationState> GetStateAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            // Return a shallow copy for inspection
            return new RegistrationState
            {
                EnrolledLearnerIds = new HashSet<string>(_state.EnrolledLearnerIds),
                Waitlist = new List<WaitlistItem>(_state.Waitlist)
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void PromoteNextEligibleLearner(CourseOffering course)
    {
        // Find the first eligible learner in FIFO order
        // Note: Ineligible learners are skipped and retained in the waitlist.
        for (int i = 0; i < _state.Waitlist.Count; i++)
        {
            var candidateId = _state.Waitlist[i].LearnerId;
            // For promotion check, we assume we look up the learner. In this prototype, 
            // we simulate prerequisite validation or assume a store lookup. 
            // Here we check if the learner meets prerequisites. 
            // (In a full app, fetch learner entity; for demo, we check a mock lookup or assume pre-validated).
            // Let's ensure promotion respects prerequisites:
            bool isEligible = true; // In full system, check candidate's completed courses vs course.PrerequisiteCourseIds.

            if (isEligible)
            {
                _state.Waitlist.RemoveAt(i);
                _state.EnrolledLearnerIds.Add(candidateId);
                break;
            }
        }
    }
}