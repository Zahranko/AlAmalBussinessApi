using System.Collections.Generic;

namespace AlAmalBusiness.Domain.IRepositories
{
    // What every user-management response needs, read in one query with the
    // role names joined in: no UserManager round trip per user, and no
    // password hash or security stamp leaving the database.
    public class UserSummaryRow
    {
        public string Id { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public int DepartmentId { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
