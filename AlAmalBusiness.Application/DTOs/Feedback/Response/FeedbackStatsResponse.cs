using System;
using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Feedback.Response
{
    // The feedback dashboard. A manager reads it for their own department; an
    // admin reads the same numbers across every department, with Departments
    // filled in so they can see which one is behind.
    //
    // Every count is over the messages *received* in the period (see
    // FeedbackStatsQuery.From/To), so the whole screen answers one question
    // rather than mixing arrival and resolution windows.
    public class FeedbackStatsResponse
    {
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }

        // Null for a manager (their own department is the only one they see),
        // set when an admin narrowed to one.
        public int? DepartmentId { get; set; }

        public int Total { get; set; }
        public int NewCount { get; set; }
        public int InReviewCount { get; set; }
        public int ResolvedCount { get; set; }
        public int ArchivedCount { get; set; }

        public int ThanksCount { get; set; }
        public int SuggestionCount { get; set; }
        public int ComplaintCount { get; set; }

        // Share of the period's messages that have been resolved, 0-100.
        public double ResolvedPercent { get; set; }

        // Arrival to resolution, in hours. Null when nothing in the period has
        // been resolved yet — which is not the same as zero, so it stays
        // nullable all the way to the browser.
        public double? AvgResolutionHours { get; set; }

        // The oldest message still sitting in New or InReview, in hours.
        public double? OldestOpenHours { get; set; }

        public List<FeedbackResolverStatResponse> Resolvers { get; set; } = new();
        public List<FeedbackDepartmentStatResponse> Departments { get; set; } = new();
    }

    // Who is closing the messages out. Counted per resolution rather than per
    // message — see FeedbackResolverRow for why.
    public class FeedbackResolverStatResponse
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public int ResolvedCount { get; set; }
        public double? AvgResolutionHours { get; set; }
    }

    public class FeedbackDepartmentStatResponse
    {
        public int DepartmentId { get; set; }
        public string? Name { get; set; }
        public int Total { get; set; }
        public int OpenCount { get; set; }
        public int ResolvedCount { get; set; }
        public int ArchivedCount { get; set; }
        public double ResolvedPercent { get; set; }
        public double? AvgResolutionHours { get; set; }
    }
}
