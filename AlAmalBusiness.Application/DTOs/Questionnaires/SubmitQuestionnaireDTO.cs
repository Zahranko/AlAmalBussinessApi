using AlAmalBusiness.Domain.Constants;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.Questionnaires
{
    // The public page's payload: one rating per question, plus an optional
    // name and phone. Arabic messages — a patient reads these.
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

        // "VeryGood" | "Good" | "Mid" | "Bad" | "VeryBad".
        public QuestionRating Rating { get; set; }
    }
}
