using AlAmalBusiness.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Questionnaires
{
    // One answer, to one question, inside one submission. Exactly one of
    // Rating and Text is filled, decided by the question's Type: neither
    // column is meaningful for the other kind, and every aggregate in this
    // feature filters on `Rating != null` rather than trusting the join to
    // sort it out.
    public class QuestionnaireAnswer
    {
        [Key]
        public int Id { get; set; }

        public int SubmissionId { get; set; }
        public QuestionnaireSubmission? Submission { get; set; }

        public int QuestionId { get; set; }
        public QuestionnaireQuestion? Question { get; set; }

        // Null on a text answer.
        public QuestionRating? Rating { get; set; }

        // Null on a rating answer, and null on a text question the patient
        // was allowed to skip — an empty box is stored as no answer at all,
        // never as an empty string, so "how many people answered this" is
        // one count with no special cases.
        [StringLength(2000)]
        public string? Text { get; set; }
    }
}
