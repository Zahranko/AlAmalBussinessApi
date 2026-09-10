using System;
using System.Collections.Generic;

namespace AlAmalBusiness.Domain.IRepositories.Feedback
{
    // What the manager/admin dashboard asks for, and the flat rows it reads
    // back. Everything here is aggregated in SQL — the dashboard must never
    // become "page through every message and count them in C#".
    public class FeedbackStatsQuery
    {
        // Inclusive bounds on the day the message *arrived* (CreatedDate),
        // not on the visit date the inbox filters by. The whole dashboard
        // reads as "of the messages received in this period…", so one date
        // meaning is used throughout rather than mixing arrival with
        // resolution dates in the same set of numbers.
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }

        // An admin narrowing to one department. Nobody else can send it.
        public int? DepartmentId { get; set; }

        // Set by the service from the caller's roles, never by the caller —
        // same contract as FeedbackListQuery's. Null means "no restriction",
        // which is Admin only.
        public int? RestrictToDepartmentId { get; set; }
    }

    public class FeedbackStatusCountRow
    {
        public Constants.FeedbackStatus Status { get; set; }
        public int Count { get; set; }
    }

    public class FeedbackTypeCountRow
    {
        public Constants.FeedbackType Type { get; set; }
        public int Count { get; set; }
    }

    // One member of staff and the messages they resolved.
    //
    // Counted per *resolution*, not per message: a message that was resolved,
    // reopened and resolved again is two pieces of work, and if two people
    // did them both are credited. That also keeps this a single grouped
    // SELECT over the timeline rather than a per-message "who touched it
    // last" lookup.
    public class FeedbackResolverRow
    {
        public string? ActorId { get; set; }
        public string? ActorName { get; set; }
        public int ResolvedCount { get; set; }
        // Minutes from the message arriving to that resolution.
        public double? AvgMinutes { get; set; }
    }

    public class FeedbackDepartmentRow
    {
        public int DepartmentId { get; set; }
        public string? Name { get; set; }
        public int Total { get; set; }
        public int NewCount { get; set; }
        public int InReviewCount { get; set; }
        public int ResolvedCount { get; set; }
        public int ArchivedCount { get; set; }
        public double? AvgMinutes { get; set; }
    }

    // Everything one dashboard load needs, read in a handful of aggregate
    // queries and assembled by the service.
    public class FeedbackStatsRows
    {
        public List<FeedbackStatusCountRow> Statuses { get; set; } = new();
        public List<FeedbackTypeCountRow> Types { get; set; } = new();
        public List<FeedbackResolverRow> Resolvers { get; set; } = new();
        public List<FeedbackDepartmentRow> Departments { get; set; } = new();

        // Across every resolved message in range, arrival to ResolvedAt.
        public double? AvgResolutionMinutes { get; set; }

        // How long the oldest message that is still open (New or InReview)
        // has been waiting — the number that says whether the queue is being
        // worked, which an average alone hides.
        public double? OldestOpenMinutes { get; set; }
    }
}
