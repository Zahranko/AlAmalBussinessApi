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
        // Staff support tickets, ported from the CRMS Tickets app. Not
        // department-scoped: anyone may raise a ticket, and one support team
        // works every department's. TUser raises tickets and follows their
        // own, TEmployee works the queue (close, comment), TManager also
        // reopens a closed ticket. TInsurance is the insurance desk: a ticket
        // whose payment method is Insurance belongs to it alone and never
        // reaches the support queue.
        public const string TManager = "TManager";
        public const string TEmployee = "TEmployee";
        public const string TUser = "TUser";
        public const string TInsurance = "TInsurance";
    }
}
