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
        public ICollection<User> Users { get; set; } = new List<User>();

    }
}
