using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Questionnaires
{
    // One patient's filled-in questionnaire. Anonymous: no name, no phone —
    // just the answers and the audit bits the server captures itself.
    public class QuestionnaireSubmission
    {
        [Key]
        public int Id { get; set; }

        public int QuestionnaireId { get; set; }
        public Questionnaire? Questionnaire { get; set; }

        public string? SubmittedFromIp { get; set; }
        public string? UserAgent { get; set; }

        // Local time — the results screen's date bounds are local day
        // boundaries, same as the feedback dashboard's.
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public ICollection<QuestionnaireAnswer> Answers { get; set; } = new List<QuestionnaireAnswer>();
    }
}
