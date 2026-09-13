using AlAmalBusiness.Domain.Constants;
using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Questionnaires
{
    // The public page's payload: one rating per question. Arabic messages —
    // a patient reads these.
    public class SubmitQuestionnaireDTO
    {
        public List<SubmitAnswerDTO> Answers { get; set; } = new();
    }

    public class SubmitAnswerDTO
    {
        public int QuestionId { get; set; }

        // "VeryGood" | "Good" | "Mid" | "Bad" | "VeryBad".
        public QuestionRating Rating { get; set; }
    }
}
