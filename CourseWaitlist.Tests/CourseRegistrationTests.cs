using CourseWaitlist.Api.Models;
using CourseWaitlist.Api.Services;
using Xunit;

namespace CourseWaitlist.Tests;

public class CourseRegistrationServiceTests
{
    [Fact]
    public async Task Enroll_SucceedsWhenCapacityAvailableAndPrereqMet()
    {
        var service = new CourseRegistrationService();
        var course = new CourseOffering { CourseId = "C1", Capacity = 1, PrerequisiteCourseIds = new() { "P1" } };
        var learner = new Learner { LearnerId = "L1", CompletedCourseIds = new() { "P1" } };

        var (success, message, status) = await service.EnrollAsync(course, learner);

        Assert.True(success);
        Assert.Equal("Enrolled", status);
        var state = await service.GetStateAsync();
        Assert.Contains("L1", state.EnrolledLearnerIds);
    }

    [Fact]
    public async Task Enroll_FailsWhenPrerequisiteNotMet()
    {
        var service = new CourseRegistrationService();
        var course = new CourseOffering { CourseId = "C1", Capacity = 1, PrerequisiteCourseIds = new() { "P1" } };
        var learner = new Learner { LearnerId = "L1", CompletedCourseIds = new() }; // Missing P1

        var (success, message, status) = await service.EnrollAsync(course, learner);

        Assert.False(success);
        Assert.Equal("Rejected_Prerequisite", status);
    }

    [Fact]
    public async Task Enroll_JoinsWaitlistWhenFullAndIdempotent()
    {
        var service = new CourseRegistrationService();
        var course = new CourseOffering { CourseId = "C1", Capacity = 1, PrerequisiteCourseIds = new() };
        var l1 = new Learner { LearnerId = "L1" };
        var l2 = new Learner { LearnerId = "L2" };

        await service.EnrollAsync(course, l1); // Fills capacity
        var (success, message, status) = await service.EnrollAsync(course, l2); // Should go to waitlist

        Assert.True(success);
        Assert.Equal("Waitlisted", status);

        // Duplicate enrollment request should remain idempotent
        var (dupSuccess, _, dupStatus) = await service.EnrollAsync(course, l2);
        Assert.True(dupSuccess);
        Assert.Equal("Waitlisted", status);

        var state = await service.GetStateAsync();
        Assert.Single(state.Waitlist);
        Assert.Equal("L2", state.Waitlist[0].LearnerId);
    }

    [Fact]
    public async Task Drop_PromotesEarliestWaitlistedLearnerFIFO()
    {
        var service = new CourseRegistrationService();
        var course = new CourseOffering { CourseId = "C1", Capacity = 1, PrerequisiteCourseIds = new() };
        var l1 = new Learner { LearnerId = "L1" };
        var l2 = new Learner { LearnerId = "L2" };
        var l3 = new Learner { LearnerId = "L3" };

        await service.EnrollAsync(course, l1);
        await service.EnrollAsync(course, l2); // waitlisted 1st
        await service.EnrollAsync(course, l3); // waitlisted 2nd

        // Drop L1, should promote L2
        await service.DropAsync(course, "L1");

        var state = await service.GetStateAsync();
        Assert.Contains("L2", state.EnrolledLearnerIds);
        Assert.DoesNotContain("L1", state.EnrolledLearnerIds);
        Assert.Single(state.Waitlist);
        Assert.Equal("L3", state.Waitlist[0].LearnerId);
    }

    [Fact]
    public async Task Drop_InvalidOrRepeatedDropHandledGracefully()
    {
        var service = new CourseRegistrationService();
        var course = new CourseOffering { CourseId = "C1", Capacity = 1 };

        var (success, message) = await service.DropAsync(course, "NON_EXISTENT");

        Assert.False(success);
    }

    [Fact]
    public async Task ConcurrentEnrollments_PreserveCapacityAndInvariants()
    {
        var service = new CourseRegistrationService();
        var course = new CourseOffering { CourseId = "C1", Capacity = 5 };

        var tasks = Enumerable.Range(1, 20).Select(i =>
        {
            var learner = new Learner { LearnerId = $"L{i}" };
            return service.EnrollAsync(course, learner);
        });

        await Task.WhenAll(tasks);

        var state = await service.GetStateAsync();
        Assert.Equal(5, state.EnrolledLearnerIds.Count);
        Assert.Equal(15, state.Waitlist.Count);
    }
}