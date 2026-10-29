using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Attendance
{
    // One letter of the attendance sheet ("D", "N", "O", ...) and the hours a
    // day marked with it is worth. Admin-maintained, like the other lookup
    // lists: a unique code, IsActive in the same body, no delete — retiring a
    // code keeps it on the days that already used it.
    //
    // The hours are read live, not snapshotted onto each day: correcting a
    // code's hours re-totals every sheet that used it.
    public class AttendanceCode
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        // What the letter means, for the dropdown ("Day shift").
        [MaxLength(100)]
        public string? Name { get; set; }

        public decimal Hours { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
