using AlAmalBusiness.Domain.Constants;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.CarChecks
{
    // One trip of a hospital car — a row of the paper "أمر حركة" (movement
    // order): who drove which car where, with whom, the odometer before and
    // after, and when it left and came back.
    //
    // The driver is whoever is signed in when the trip is started (DriverId),
    // never typed. The two times are stamped by the server when the driver
    // presses "depart now" / "return now", so they can't be back-dated.
    //
    // A trip is a draft its driver may still change until they submit it;
    // once IsSubmitted it is locked for everyone but an Admin, who may change
    // any field including the times (LastEditedBy* records that they did).
    public class CarTrip
    {
        [Key]
        public int Id { get; set; }

        public int CarId { get; set; }
        public SecurityCar? Car { get; set; }

        // اسم السائق
        public string DriverId { get; set; } = string.Empty;
        public User? Driver { get; set; }

        // اسم المرافق — free text, most companions have no account.
        [MaxLength(100)]
        public string? CompanionName { get; set; }

        // المكان المتجه له
        [MaxLength(200)]
        public string? Destination { get; set; }

        // العداد الحالي / العداد القادم (km). المسافة المقطوعة = End - Start.
        public int? StartOdometer { get; set; }
        public int? EndOdometer { get; set; }

        // موعد الانطلاق / موعد العودة — Jordan time, server-stamped.
        public DateTime? DepartedAt { get; set; }
        public DateTime? ReturnedAt { get; set; }

        public bool IsSubmitted { get; set; }
        public DateTime? SubmittedAt { get; set; }

        public DateTime CreatedAt { get; set; } = AppClock.Now;

        // Set when an Admin changes a submitted trip.
        public string? LastEditedById { get; set; }
        public User? LastEditedBy { get; set; }
        public DateTime? LastEditedAt { get; set; }
    }
}
