using AlAmalBusiness.Application.DTOs.Questionnaires.Response;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.Questionnaires
{
    // The monthly questionnaire report: one email per department, to every
    // active QManager of that department with an email address, summarising
    // `month` against every month before it, with each questionnaire's
    // workbook (charts included) attached.
    public interface IQuestionnaireMonthlyReportService
    {
        // The scheduler's entry point: sends `year/month` once, ever. Skips
        // (and says why) if the month was already sent or email is off.
        Task<MonthlyReportSendResult> SendScheduledAsync(int year, int month);

        // An admin's manual (re)send — ignores the once-per-month log.
        Task<MonthlyReportSendResult> SendNowAsync(int year, int month);

        // The email exactly as it would be sent, for every department (or one).
        Task<string> RenderPreviewAsync(int year, int month, int? departmentId);

        // The workbook attached for one questionnaire in that month's email.
        Task<(string FileName, byte[] Content)?> BuildAttachmentAsync(int questionnaireId, int year, int month);
    }
}
