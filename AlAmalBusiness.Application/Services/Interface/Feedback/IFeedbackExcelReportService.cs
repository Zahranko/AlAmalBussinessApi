using AlAmalBusiness.Application.DTOs.Feedback.Response;

namespace AlAmalBusiness.Application.Services.Interface.Feedback
{
    public interface IFeedbackExcelReportService
    {
        // The dashboard's numbers as a workbook. Whatever scoping the caller
        // is under has already been applied to the stats — a manager arrives
        // here holding one department's rows, an admin every department's —
        // so this only renders what it is given.
        byte[] Build(FeedbackStatsResponse stats);
    }
}
