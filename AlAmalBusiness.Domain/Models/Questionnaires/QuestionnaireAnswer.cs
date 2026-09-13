using AlAmalBusiness.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Questionnaires
{
    // One rating, for one question, inside one submission.
    public class QuestionnaireAnswer
    {
        [Key]
        public int Id { get; set; }

        public int SubmissionId { get; set; }
        public QuestionnaireSubmission? Submission { get; set; }

        public int QuestionId { get; set; }
        public QuestionnaireQuestion? Question { get; set; }

        public QuestionRating Rating { get; set; }
    }
}
