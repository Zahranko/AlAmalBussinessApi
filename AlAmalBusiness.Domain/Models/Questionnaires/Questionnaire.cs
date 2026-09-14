using AlAmalBusiness.Domain.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Questionnaires
{
    // A patient questionnaire page, e.g. the radiology one patients open at
    // <public site>/radiology.
    //
    // The folder/namespace is plural (Questionnaires) so this type doesn't
    // collide with its own namespace, the way PatientFeedback had to be named
    // around Models.Feedback.
    //
    // DepartmentId points at the shared Departments lookup — the same column
    // User.DepartmentId uses, which is what makes "a QManager sees their own
    // department's questionnaires" a plain comparison.
    public class Questionnaire
    {
        [Key]
        public int Id { get; set; }

        // Shown to the patient as the page heading.
        [Required]
        public string Title { get; set; } = string.Empty;

        // The page's public route segment. Lowercase latin letters, digits and
        // single hyphens (see QuestionnaireService.NormalizeSlug); unique.
        [Required]
        public string Slug { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public int DepartmentId { get; set; }
        public Departments? Department { get; set; }

        // An inactive questionnaire keeps its answers but its public page
        // answers "not found".
        public bool IsActive { get; set; } = true;

        public string? CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        // Local time, matching Lead/PatientFeedback.CreatedDate.
        public DateTime CreatedDate { get; set; } = AppClock.Now;

        public ICollection<QuestionnaireQuestion> Questions { get; set; } = new List<QuestionnaireQuestion>();
    }
}
