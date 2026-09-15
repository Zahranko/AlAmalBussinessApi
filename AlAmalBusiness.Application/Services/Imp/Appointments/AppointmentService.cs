using AlAmalBusiness.Application.DTOs.Appointments;
using AlAmalBusiness.Application.DTOs.Appointments.Response;
using AlAmalBusiness.Application.DTOs.Feedback;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Application.Services.Interface.Appointments;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.Appointments;
using AlAmalBusiness.Domain.Models.Appointments;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.Appointments
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRequestRepo _requestRepo;
        private readonly IAppointmentProcedureRepo _procedureRepo;
        private readonly IAppointmentReferralSourceRepo _referralSourceRepo;
        private readonly IAppointmentEmailRepo _emailRepo;
        private readonly IEmailQueue _emailQueue;
        private readonly ILogger<AppointmentService> _logger;

        private const string DefaultCountryCode = "+962";

        public AppointmentService(
            IAppointmentRequestRepo requestRepo,
            IAppointmentProcedureRepo procedureRepo,
            IAppointmentReferralSourceRepo referralSourceRepo,
            IAppointmentEmailRepo emailRepo,
            IEmailQueue emailQueue,
            ILogger<AppointmentService> logger)
        {
            _requestRepo = requestRepo;
            _procedureRepo = procedureRepo;
            _referralSourceRepo = referralSourceRepo;
            _emailRepo = emailRepo;
            _emailQueue = emailQueue;
            _logger = logger;
        }

        public async Task<AppointmentFormOptionsResponse> GetFormOptionsAsync()
        {
            // Sequential on purpose: both run on the one scoped DbContext,
            // which does not allow concurrent queries.
            var procedures = await _procedureRepo.GetActiveAsync();
            var referralSources = await _referralSourceRepo.GetActiveAsync();

            return new AppointmentFormOptionsResponse
            {
                Procedures = procedures.Select(p => new AppointmentOptionResponse { Id = p.Id, Name = p.Name }).ToList(),
                ReferralSources = referralSources.Select(r => new AppointmentOptionResponse { Id = r.Id, Name = r.Name }).ToList()
            };
        }

        public async Task<AppointmentCreatedResponse> SubmitAsync(CreateAppointmentRequestDTO request, SubmissionContext context)
        {
            var fullName = Clean(request.FullName);
            if (fullName == null || fullName.Length < 2)
                return Rejected("الاسم مطلوب (حرفان على الأقل)");

            // Inactive entries answer the same as missing ones: a retired
            // choice stays on old requests but can't be picked again.
            var procedure = await _procedureRepo.GetByIdAsync(request.ProcedureId);
            if (procedure == null || !procedure.IsActive)
                return Rejected("الإجراء المختار غير متاح");

            var referralSource = await _referralSourceRepo.GetByIdAsync(request.ReferralSourceId);
            if (referralSource == null || !referralSource.IsActive)
                return Rejected("الخيار المختار في «كيف سمعت عنا» غير متاح");

            var appointment = new AppointmentRequest
            {
                FullName = fullName,
                PhoneCountryCode = NormalizeCountryCode(request.PhoneCountryCode),
                PhoneNumber = NormalizeNationalNumber(request.PhoneNumber),
                ProcedureId = procedure.Id,
                ReferralSourceId = referralSource.Id,
                Details = Clean(request.Details),
                SubmittedFromIp = context.IpAddress,
                UserAgent = Truncate(context.UserAgent, 400),
                CreatedDate = AppClock.Now
            };

            await _requestRepo.CreateAsync(appointment);

            await NotifyAsync(appointment, procedure.Name, referralSource.Name);

            return new AppointmentCreatedResponse { Success = true, CreatedDate = appointment.CreatedDate };
        }

        // One email per active address on the emails list. Best-effort, as
        // with feedback: the request is already saved, so nothing here may
        // fail the patient's submit — the SMTP send itself happens later on
        // the email queue's background worker.
        private async Task NotifyAsync(AppointmentRequest appointment, string procedureName, string referralSourceName)
        {
            try
            {
                var recipients = await _emailRepo.GetActiveAddressesAsync();
                if (recipients.Count == 0)
                {
                    _logger.LogInformation(
                        "Appointment request {Id}: the notification emails list has no active address, no email sent.",
                        appointment.Id);
                    return;
                }

                foreach (var to in recipients)
                {
                    if (!_emailQueue.Enqueue(AppointmentEmailTemplate.Build(to, appointment, procedureName, referralSourceName)))
                        _logger.LogWarning("Appointment request {Id}: email to {To} was not queued.", appointment.Id, to);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Appointment request {Id}: failed to queue the notification email.", appointment.Id);
            }
        }

        // ---------- helpers (same phone rules as FeedbackService) ----------

        private static string DigitsOnly(string value) => Regex.Replace(value, @"\D", string.Empty);

        // Drops the national leading zero (0790... -> 790...) so the number
        // isn't +9620790... once the country code is prepended.
        private static string NormalizeNationalNumber(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var digits = DigitsOnly(value);
            var trimmed = digits.TrimStart('0');
            return trimmed.Length == 0 ? digits : trimmed;
        }

        private static string NormalizeCountryCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return DefaultCountryCode;
            var digits = DigitsOnly(code);
            return digits.Length == 0 ? DefaultCountryCode : $"+{digits}";
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? Truncate(string? value, int max) =>
            string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];

        private static AppointmentCreatedResponse Rejected(string error) =>
            new() { Success = false, Error = error };
    }
}
