using AlAmalBusiness.Domain.Constants;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Questionnaires
{
    // One row per monthly QManager report that went out (Year/Month = the month
    // being reported on). Unique on (Year, Month): the background job claims a
    // month by inserting its row before sending, so an app-pool restart, a
    // second worker process or an hourly re-check can never email the same
    // month twice. A manual resend from the console doesn't write here.
    public class QuestionnaireReportRun
    {
        [Key]
        public int Id { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }

        public DateTime StartedAt { get; set; } = AppClock.Now;
        public DateTime? CompletedAt { get; set; }

        public int EmailsQueued { get; set; }
    }
}
