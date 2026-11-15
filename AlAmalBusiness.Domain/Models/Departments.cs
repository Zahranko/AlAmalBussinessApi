using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AlAmalBusiness.Domain.Models
{
    public class Departments
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string? Name { get; set; }
        public bool IsActive { get; set; } = true;

        // Position in the public feedback form's department dropdown
        // (1-based, contiguous). Admin-controlled from the console's
        // Settings -> Departments screen; new departments land last.
        public int DisplayOrder { get; set; }

        // Position in the attendance screens (the month's sheet list and the
        // open-month dialog) — nursing first, security second and so on.
        // Its own column on purpose: the public form's order is for patients,
        // this one is the HR grouping, and moving one must not move the other.
        public int AttendanceOrder { get; set; }

        // Whether patients can pick this department on each public page
        // (2026-11-15): the appointment page (appointment.) and the feedback
        // page (feedback.). Admin switches them from Settings -> Departments.
        // A hidden department is left out of that page's list and refused on
        // submit there; staff screens and forwarding still use it. Only an
        // active department is ever shown, whatever these say.
        public bool ShowOnAppointment { get; set; } = true;
        public bool ShowOnFeedback { get; set; } = true;
        public ICollection<User> Users { get; set; } = new List<User>();

    }
}
