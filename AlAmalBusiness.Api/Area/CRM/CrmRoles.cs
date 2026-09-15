using AlAmalBusiness.Domain.Constants;

namespace AlAmalBusiness.Api.Area.CRM
{
    // Every role that works the CRM, for [Authorize(Roles = ...)] on CRM
    // surfaces that aren't a controller with its own list (the lead hub, the
    // active-lookup endpoints the case screens read).
    public static class CrmRoles
    {
        public const string Access = AppRoles.CManager + "," + AppRoles.CEmployee + "," + AppRoles.CUser + "," + AppRoles.Admin;
    }
}
