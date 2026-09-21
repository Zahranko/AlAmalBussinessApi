using AlAmalBusiness.Domain.Constants;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.Questionnaires
{
    // The public page's payload: one answer per question — a rating or a
    // line of text, following the question's own type — plus an optional
    // name and phone. Arabic messages: a patient reads these.
    public class SubmitQuestionnaireDTO
    {
        public List<SubmitAnswerDTO> Answers { get; set; } = new();

        [StringLength(100, ErrorMessage = "الاسم يجب ألا يتجاوز 100 حرف")]
        public string? Name { get; set; }

        // Checked for 7-15 digits in QuestionnaireService once separators are
        // stripped; this only keeps junk out.
        [RegularExpression(@"^[0-9+\s\-()]{0,20}$", ErrorMessage = "رقم الهاتف غير صحيح")]
        public string? PhoneNumber { get; set; }

        [StringLength(2000, ErrorMessage = "الملاحظات يجب ألا تتجاوز 2000 حرف")]
        public string? Notes { get; set; }
    }

    public class SubmitAnswerDTO
    {
        public int QuestionId { get; set; }

        // "VeryGood" | "Good" | "Mid" | "Bad" | "VeryBad". Null on a text
        // question, where Text carries the answer instead.
        public QuestionRating? Rating { get; set; }

        // The answer to a text question. Blank counts as unanswered, which
        // an optional question allows and a required one refuses.
        [StringLength(2000, ErrorMessage = "الإجابة يجب ألا تتجاوز 2000 حرف")]
        public string? Text { get; set; }
    }
}
