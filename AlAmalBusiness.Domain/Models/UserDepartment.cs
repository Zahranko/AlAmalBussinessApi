namespace AlAmalBusiness.Domain.Models
{
    // A department this user may READ, on top of the one they work in
    // (User.DepartmentId). The two are different facts: the home department
    // says where someone works and is what every write stamps (a ticket
    // records the department that raised it, a new questionnaire lands in
    // one), while these rows only widen what they are allowed to see.
    //
    // No rows means the reach is the home department alone, which is how
    // every account behaved before this table existed. The effective set is
    // always the union of the two, so a grant here can never cost someone
    // their own department.
    public class UserDepartment
    {
        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public int DepartmentId { get; set; }
        public Departments Department { get; set; } = null!;
    }
}
