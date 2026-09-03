using AlAmalBusiness.Domain.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace AlAmalBusiness.Domain.Models.CRM
{
    public class Lead
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; } 
        public string? CountryKey { get; set; }
        public string? PhoneNum { get; set; }
        public string? NickName { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public PaymentWays? PaymentWay { get; set; }
        public LeadStatus Status { get; set; } = LeadStatus.New; 

        public bool HasDoctor { get; set; }=false;
        public int? DoctorId { get; set; }
        public Doctors? Doctor { get; set; } = null!;
        [Required]
        public int ReferalId { get; set; }
        public ReferalSource? Referal { get; set; } 
        [Required]
        public int ProcedureId { get; set; }
        public Procedures? Procedure { get; set; } 
        [Required]
        public string? CreatedById { get; set; } 
        public User? CreatedBy { get; set; }

        public string? ClaimedById { get; set; }
        public User? ClaimedBy { get; set; }

        public DateTime? AppointmentDate { get; set; }
        public string? ClinicSignature { get; set; }

        public int? ClosedReasonId { get; set; }
        public ClosedReason? ClosedReason { get; set; }




    }
}
