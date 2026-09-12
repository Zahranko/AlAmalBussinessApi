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
        public ICollection<User> Users { get; set; } = new List<User>();

    }
}
