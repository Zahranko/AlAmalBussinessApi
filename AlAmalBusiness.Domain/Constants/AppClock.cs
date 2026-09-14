using System;

namespace AlAmalBusiness.Domain.Constants
{
    // The one clock for every business timestamp: Jordan (Asia/Amman) time.
    //
    // Never use DateTime.Now in this solution. Production runs on SmarterASP,
    // whose web and SQL servers are on Central European time (UTC+1/+2), so
    // DateTime.Now was 1-2 hours behind Amman and DateTime.UtcNow 3 hours
    // behind — and the console shows stored values as-is (no offset in the
    // JSON), so staff saw both. Found and fixed 2026-09-14.
    //
    // A fixed offset rather than a TimeZoneInfo lookup on purpose: Jordan has
    // been UTC+3 all year since October 2022, and an unpatched Windows host
    // would still apply the old "Jordan Standard Time" winter rule (+2).
    // If Jordan ever brings DST back, this is the only line to change.
    //
    // Auth internals (JWT expiry, RefreshToken timestamps) stay on
    // DateTime.UtcNow — they are never shown and only compared to each other.
    public static class AppClock
    {
        public static readonly TimeSpan JordanOffset = TimeSpan.FromHours(3);

        public static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow + JordanOffset, DateTimeKind.Unspecified);

        public static DateTime Today => Now.Date;
    }
}
