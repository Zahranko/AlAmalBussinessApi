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
    }
}
