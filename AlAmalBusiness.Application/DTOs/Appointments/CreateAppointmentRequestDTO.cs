using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.Appointments
{
    // The public appointment page's payload. Arabic messages — a patient
    // reads these validation errors.
    public class CreateAppointmentRequestDTO
    {
        [Required(ErrorMessage = "الاسم مطلوب")]
        [StringLength(120, MinimumLength = 2, ErrorMessage = "الاسم يجب أن يكون بين 2 و 120 حرفاً")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [RegularExpression(@"^[0-9\s\-]{7,15}$", ErrorMessage = "رقم الهاتف غير صحيح")]
        public string? PhoneNumber { get; set; }

        [StringLength(6)]
        public string? PhoneCountryCode { get; set; } = "+962";

        [Range(1, int.MaxValue, ErrorMessage = "يرجى اختيار الإجراء")]
        public int ProcedureId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "يرجى اختيار كيف سمعت عنا")]
        public int ReferralSourceId { get; set; }

        [StringLength(2000, ErrorMessage = "التفاصيل يجب ألا تتجاوز 2000 حرف")]
        public string? Details { get; set; }
    }
}
