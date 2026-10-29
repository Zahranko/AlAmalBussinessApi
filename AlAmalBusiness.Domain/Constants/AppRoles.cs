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
        // Patient feedback: reads and works their own department's inbox.
        // Admin is the only cross-department reader.
        //
        // (FEmployee and FUser were removed 2026-09-20. The three-tier shape
        // came across from the old app's capability flags and was never used
        // here — nobody held either role — so a department's feedback is one
        // job now, and FManager is it.)
        public const string FManager = "FManager";
        // Patient questionnaires: builds and reads the questionnaires of
        // their own department. Admin is the only cross-department reader.
        public const string QManager = "QManager";
        // Appointment requests: the same per-department role as FManager, for
        // the public appointment page's inbox. An AManager is who the
        // new-request email goes to (AppointmentService), one department each.
        //
        // (AEmployee and AUser were removed 2026-09-20 with F*'s pair, for
        // the same reason — this area was copied from feedback, tiers and
        // all.)
        public const string AManager = "AManager";
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
        // Mammogram follow-up register (added 2026-10-15): opens the monthly
        // sheet, fills it and works its follow-up list. Not per-department —
        // it is the one radiology team's register.
        public const string MManager = "MManager";
        // Maintenance tickets (added 2026-10-21): the support tickets'
        // raise/solve split for building and equipment faults. MRaiser raises
        // requests and follows only their own; MSupport is the maintenance
        // team, receiving and solving every request from every department.
        // Neither is per-department. (The M- prefix is shared with MManager
        // only by spelling — that one is the mammogram register's.)
        public const string MRaiser = "MRaiser";
        public const string MSupport = "MSupport";
        // Staff attendance (added 2026-10-29): opens a department's monthly
        // attendance sheet and fills its codes. Per-department, the feedback
        // way — the departments on their token, Admin every department. Spelt
        // out in full because every short prefix (A-, M-) is already taken.
        public const string AttendanceManager = "AttendanceManager";
    }
}
