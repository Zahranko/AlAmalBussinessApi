using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Questionnaires
{
    // One question on a questionnaire. Every question is answered on the same
    // five-point QuestionRating scale, so there is no per-question answer set.
    public class QuestionnaireQuestion
    {
        [Key]
        public int Id { get; set; }

        public int QuestionnaireId { get; set; }
        public Questionnaire? Questionnaire { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;

        // 1-based position on the page.
        public int DisplayOrder { get; set; }

        // A question removed after patients already answered it. It leaves the
        // public page and the edit form, but stays in the table so the answers
        // it collected — and the results screen's history — stay intact. A
        // question nobody answered yet is deleted outright instead.
        public bool IsArchived { get; set; }

        public ICollection<QuestionnaireAnswer> Answers { get; set; } = new List<QuestionnaireAnswer>();
    }
}
