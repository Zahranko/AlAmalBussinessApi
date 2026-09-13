using AlAmalBusiness.Application.DTOs.Questionnaires.Response;
using AlAmalBusiness.Domain.IRepositories.Questionnaires;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlAmalBusiness.Application.Services.Imp.Questionnaires
{
    // Turns per-month aggregate rows into what the charts draw: a contiguous
    // run of months ending at the report month, plus "the report month" vs
    // "every month before it" — shared by the Excel export and the monthly
    // email so the two can never disagree about the numbers.
    internal static class QuestionnaireTrendBuilder
    {
        public static QuestionnaireTrend Build(IEnumerable<QuestionnaireMonthRow> rows, int year, int month, int maxMonths)
        {
            // Several questionnaires fold into one line per month (a department's).
            var byMonth = rows
                .GroupBy(r => (r.Year, r.Month))
                .ToDictionary(g => g.Key, g => new Acc(g.Sum(r => r.Submissions), g.Sum(r => r.AnswerCount), g.Sum(r => r.RatingSum), g.Sum(r => r.Positive)));

            var end = new DateTime(year, month, 1);

            // Start at the first month that has anything, but never show more
            // than maxMonths — a long history would squash the chart.
            var first = byMonth.Keys
                .Select(k => new DateTime(k.Year, k.Month, 1))
                .Where(d => d <= end)
                .DefaultIfEmpty(end)
                .Min();
            var start = first < end.AddMonths(-(maxMonths - 1)) ? end.AddMonths(-(maxMonths - 1)) : first;

            var trend = new QuestionnaireTrend();
            for (var d = start; d <= end; d = d.AddMonths(1))
            {
                byMonth.TryGetValue((d.Year, d.Month), out var acc);
                trend.Months.Add(new MonthPointResponse
                {
                    Year = d.Year,
                    Month = d.Month,
                    Label = Label(d.Year, d.Month),
                    Submissions = acc?.Submissions ?? 0,
                    AverageRating = acc?.Average,
                    SatisfactionPercent = acc?.Satisfied
                });
            }

            byMonth.TryGetValue((year, month), out var current);
            trend.ReportMonth = Summary(Label(year, month), current);

            // "All previous months" is every month before, not only the ones
            // that fit on the chart.
            var before = byMonth.Where(kv => new DateTime(kv.Key.Year, kv.Key.Month, 1) < end).Select(kv => kv.Value).ToList();
            trend.PreviousMonths = Summary(
                "All previous months",
                before.Count == 0 ? null : new Acc(before.Sum(a => a.Submissions), before.Sum(a => a.Answers), before.Sum(a => a.RatingSum), before.Sum(a => a.Positive)));

            return trend;
        }

        public static string Label(int year, int month) => $"{year:D4}-{month:D2}";

        private static PeriodSummaryResponse Summary(string label, Acc? acc) => new()
        {
            Label = label,
            Submissions = acc?.Submissions ?? 0,
            AverageRating = acc?.Average,
            SatisfactionPercent = acc?.Satisfied
        };

        private sealed record Acc(int Submissions, int Answers, int RatingSum, int Positive)
        {
            public double? Average => Answers == 0 ? null : Math.Round((double)RatingSum / Answers, 2);
            public double? Satisfied => Answers == 0 ? null : Math.Round(Positive * 100.0 / Answers, 1);
        }
    }
}
