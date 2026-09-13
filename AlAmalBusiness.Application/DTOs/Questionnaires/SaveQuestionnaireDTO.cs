using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.Questionnaires
{
    // Create and update share one body: the questionnaire and its whole
    // question list, in page order. Update is a full replace of the list —
    // a question left out is removed (or archived, if it has answers).
    public class SaveQuestionnaireDTO
    {
        [Required(ErrorMessage = "عنوان الاستبيان مطلوب")]
        [StringLength(200, ErrorMessage = "العنوان يجب ألا يتجاوز 200 حرف")]
        public string? Title { get; set; }

        // The public route segment: /radiology. Normalised to lowercase
        // server-side; see QuestionnaireService.NormalizeSlug for the rules.
        [Required(ErrorMessage = "رابط الاستبيان مطلوب")]
        [StringLength(60, ErrorMessage = "الرابط يجب ألا يتجاوز 60 حرفاً")]
        public string? Slug { get; set; }

        [StringLength(1000, ErrorMessage = "الوصف يجب ألا يتجاوز 1000 حرف")]
        public string? Description { get; set; }

        // Honoured for an Admin only. A QManager's questionnaires always
        // belong to the department on their own token, whatever is sent here.
        public int? DepartmentId { get; set; }

        public bool IsActive { get; set; } = true;

        public List<SaveQuestionDTO> Questions { get; set; } = new();
    }

    public class SaveQuestionDTO
    {
        // Null for a new question; an existing question's id to keep it (and
        // the answers it already collected) while editing its text or order.
        public int? Id { get; set; }

        [Required(ErrorMessage = "نص السؤال مطلوب")]
        [StringLength(500, ErrorMessage = "نص السؤال يجب ألا يتجاوز 500 حرف")]
        public string? Text { get; set; }
    }
}
