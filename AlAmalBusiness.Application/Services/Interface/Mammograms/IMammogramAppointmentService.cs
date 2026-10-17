using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Mammograms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.Mammograms
{
    public interface IMammogramAppointmentService
    {
        Task<PagedResultDTO<MammogramAppointmentResponse>> GetPagedAsync(MammogramAppointmentListQueryDTO query, string userId);
        Task<List<MammogramAppointmentResponse>> GetDayAsync(DateOnly day);
        Task<MammogramAppointmentDetailResponse?> GetDetailAsync(int id);

        Task<MammogramResult<MammogramAppointmentDetailResponse>> BookAsync(BookMammogramAppointmentDTO dto, string userId);
        Task<MammogramResult<MammogramAppointmentDetailResponse>> FollowUpAsync(int id, MammogramAppointmentFollowUpDTO dto, string userId);

        // The patient is here. Only once the appointment's day has come.
        Task<MammogramResult<MammogramAppointmentDetailResponse>> ArriveAsync(int id, string userId);

        Task<MammogramAppointmentStatsResponse> GetStatsAsync(int? year, int? month);
        Task<MammogramResult<(byte[] Content, string FileName)>> ExportAsync(DateOnly? from, DateOnly? to);

        Task<List<string>> GetDoctorNamesAsync();
    }
}
