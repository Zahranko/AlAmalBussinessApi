using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.DTOs.Departments
{
    public class DepartmentDTO
    {
        public int Id { get; set; }
        public string? Name{ get; set; }
        public bool IsActive { get; set; } = true;

        // Where this department sits in the public feedback form's dropdown.
        // Read-only as far as Create/Update are concerned — only the
        // reorder endpoint writes it, so an edit can't silently move a row.
        public int DisplayOrder { get; set; }

        // Where it sits on the attendance screens. Read-only here too; only
        // PUT /api/Department/attendance-order writes it.
        public int AttendanceOrder { get; set; }
    }
}
