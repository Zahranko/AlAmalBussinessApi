using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Domain.Constants
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string CManager = "CManager";
        public const string CEmployee = "CEmployee";
        public const string CUser = "CUser";
        public const string FManager = "FManager";
        public const string FEmployee = "FEmployee";
        public const string FUser = "FUser";
        // Patient questionnaires: builds and reads the questionnaires of
        // their own department. Admin is the only cross-department reader.
        public const string QManager = "QManager";
        // Appointment requests: the same per-department tier as F*, for the
        // public appointment page's inbox. An AManager is who the new-request
        // email goes to (AppointmentService), one department each.
        public const string AManager = "AManager";
        public const string AEmployee = "AEmployee";
        public const string AUser = "AUser";
        // Staff support tickets. Reshaped 2026-09-19: raising and solving are
        // now different jobs held by different people, and the two raising
        // roles are department-scoped where they were not before.
        //
        // TEmployee raises tickets and follows only their own. TManager
        // raises too, and reads every ticket raised inside their own
        // department — a supervisor's view, not a worker's: neither role
        // closes anything.
        //
        // TSupport is the support agent, and the only not-per-department
        // ticket role: it receives and solves every ticket that isn't flagged
        // insurance, whichever department raised it. TInsurance is the
        // insurance desk and owns the flagged ones outright — nobody else
        // sees them, the support agent included, exactly as the old
        // payment-method split behaved.
        //
        // (TUser was removed the same day: TEmployee is now what it was.)
        public const string TManager = "TManager";
        public const string TEmployee = "TEmployee";
        public const string TSupport = "TSupport";
        public const string TInsurance = "TInsurance";
    }
}
