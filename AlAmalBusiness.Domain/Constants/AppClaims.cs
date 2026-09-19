using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlAmalBusiness.Domain.Constants
{
    // Non-standard claims this app puts on its own access tokens, on top of
    // the JWT registered ones (sub / unique_name / name) and role.
    public static class AppClaims
    {
        // The user's home department id — where they work. Every write that
        // records a department reads this one (a ticket's originating
        // department, where a new questionnaire lands). It is read from the
        // signed token rather than the request body because it is an
        // authorization input, not decoration. Like roles, a change to it
        // takes effect on the next refresh (<= 15 min) rather than whenever
        // the browser is next closed.
        public const string DepartmentId = "department_id";

        // Every department the user may READ, comma-separated: the home
        // department plus whatever UserDepartments grants them. This is what
        // scopes the feedback, appointment and questionnaire screens, so it
        // is as much an authorization input as the roles beside it, and it is
        // resolved once when the token is issued — no request ever joins
        // through the table to find it.
        //
        // A token issued before this claim existed simply doesn't carry it;
        // the parser below then answers an empty set, and the caller falls
        // back to their home department alone — which is exactly the old
        // behavior, until their next refresh.
        public const string DepartmentIds = "department_ids";

        // The claim's value as ids. Anything unparseable or non-positive is
        // dropped rather than throwing: a malformed token should narrow what
        // its holder can see, never widen it or 500 the request.
        public static List<int> ParseDepartmentIds(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return new List<int>();

            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => int.TryParse(part, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }

        // The set a caller is actually scoped to: what the claim carries, or
        // the home department alone when it carries nothing. Kept here so all
        // three areas resolve it the same way.
        public static List<int> ReadableDepartmentIds(string? claimValue, int homeDepartmentId)
        {
            var ids = ParseDepartmentIds(claimValue);

            if (homeDepartmentId > 0 && !ids.Contains(homeDepartmentId))
                ids.Add(homeDepartmentId);

            ids.Sort();
            return ids;
        }
    }
}
