using AlAmalBusiness.Domain.Constants;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Questionnaires
{
    // One question on a questionnaire. Until 2026-09-21 every question was
    // answered on the same five-point QuestionRating scale; a question now
    // also comes in a free-text flavour (Type), and the two are different
    // enough that almost everything downstream branches on it — a text
    // answer has no score, so it is in no average, no distribution and no
    // trend.
    public class QuestionnaireQuestion
    {
        [Key]
        public int Id { get; set; }

        public int QuestionnaireId { get; set; }
        public Questionnaire? Questionnaire { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;

        // Rating (the five-point scale) or Text (free prose). Rating is 0, so
        // every question written before this column existed is one.
        public QuestionType Type { get; set; } = QuestionType.Rating;

        // Whether the patient must answer. A Rating question always must —
        // a half-filled scale skews the per-question averages against each
        // other — so this only ever says anything about a Text question,
        // where the builder decides per question.
        public bool IsRequired { get; set; } = true;

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
