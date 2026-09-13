using AlAmalBusiness.Application.DTOs.Questionnaires.Response;

namespace AlAmalBusiness.Application.Services.Interface.Questionnaires
{
    public interface IQuestionnaireExcelReportService
    {
        // One questionnaire's results: summary, per-question numbers and every
        // response. The data arrives already scoped by QuestionnaireService —
        // this only renders what it is given.
        byte[] Build(QuestionnaireExportData data);

        // The questionnaires list as a single sheet — every questionnaire the
        // caller can see, with its status and numbers over the period.
        byte[] BuildOverview(QuestionnaireListResponse list);
    }
}
