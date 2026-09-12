using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Departments
{
    // Body of PUT /api/Department/reorder: every department id, in the order
    // the public feedback form should list them. Deliberately the whole list
    // rather than a single "move this one to position N" — the console sends
    // what it is showing, so a stale list is caught instead of writing an
    // order the admin never saw.
    public class ReorderDepartmentsDTO
    {
        public List<int> DepartmentIds { get; set; } = new List<int>();
    }
}
