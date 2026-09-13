using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Questionnaires
{
    // One patient's filled-in questionnaire. Anonymous unless the patient
    // chooses to leave a name and/or phone number — both optional.
    public class QuestionnaireSubmission
    {
        [Key]
        public int Id { get; set; }

        public int QuestionnaireId { get; set; }
        public Questionnaire? Questionnaire { get; set; }

        public string? Name { get; set; }

        // Digits only (a leading + kept), as the patient typed it otherwise —
        // no country picker on the page, so no national-zero stripping.
        public string? PhoneNumber { get; set; }

        // Optional free text from the box at the bottom of the page.
        public string? Notes { get; set; }

        public string? SubmittedFromIp { get; set; }
        public string? UserAgent { get; set; }

        // Local time — the results screen's date bounds are local day
        // boundaries, same as the feedback dashboard's.
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public ICollection<QuestionnaireAnswer> Answers { get; set; } = new List<QuestionnaireAnswer>();
    }
}
