using AlAmalBusiness.Domain.Constants;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.Feedback
{
    // The public form's payload. The messages are Arabic because this is the
    // only DTO a patient ever sees validation errors from.
    public class CreateFeedbackDTO
    {
        [Required(ErrorMessage = "نوع الرسالة مطلوب")]
        [EnumDataType(typeof(FeedbackType), ErrorMessage = "نوع الرسالة غير صحيح")]
        public FeedbackType Type { get; set; }

        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "الاسم الأول يجب أن يكون بين 2 و 60 حرفاً")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "اسم العائلة يجب أن يكون بين 2 و 60 حرفاً")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [RegularExpression(@"^[0-9\s\-]{7,15}$", ErrorMessage = "رقم الهاتف غير صحيح")]
        public string? PhoneNumber { get; set; }

        [StringLength(6)]
        public string? PhoneCountryCode { get; set; } = "+962";

        [Required(ErrorMessage = "التاريخ مطلوب")]
        public DateOnly VisitDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "القسم مطلوب")]
        public int DepartmentId { get; set; }

        [StringLength(2000, ErrorMessage = "التفاصيل يجب ألا تتجاوز 2000 حرف")]
        public string? Details { get; set; }
    }
}
