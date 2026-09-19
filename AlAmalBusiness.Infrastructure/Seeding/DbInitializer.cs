using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace AlAmalBusiness.Infrastructure.Seeding
{
    public class DbInitializer
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AlAmalBusiness.DbContext.Infrastructure.AppDbContext _context;
        public DbInitializer(RoleManager<IdentityRole> roleManager, AlAmalBusiness.DbContext.Infrastructure.AppDbContext context)
        {
            _roleManager = roleManager;
            _context = context;
        }

        // The three ticket procedures the workflow is built around, seeded
        // rather than left to an admin to type: "Open invoice" is the one a
        // ticket may be flagged insurance on, and that rule reads
        // AllowsInsurance, so the row has to exist and carry the flag for the
        // insurance desk to receive anything at all.
        //
        // Matched by name and only ever inserted, never updated or removed:
        // an admin may rename or retire any of them afterwards without this
        // undoing it on the next cold start. AllowsInsurance is set on insert
        // alone for the same reason — it is theirs to change from Settings.
        private static readonly (string Name, bool AllowsInsurance)[] Procedures =
        {
            ("حذف", false),
            ("خصم", false),
            ("فتح فاتورة", true),
        };

        public async Task SeedTicketProceduresAsync()
        {
            var existing = await _context.TicketProcedures
                .Select(p => p.Name)
                .ToListAsync();

            var added = false;
            foreach (var (name, allowsInsurance) in Procedures)
            {
                if (existing.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;

                _context.TicketProcedures.Add(new Domain.Models.Tickets.TicketProcedure
                {
                    Name = name,
                    IsActive = true,
                    AllowsInsurance = allowsInsurance
                });
                added = true;
            }

            if (added) await _context.SaveChangesAsync();
        }
        public async Task SeedRolesAsync()
        {
            string[] Roles = new string[]
            {
                AppRoles.Admin,
                AppRoles.CManager,
                AppRoles.CEmployee,
                AppRoles.CUser,
                AppRoles.FManager,
                AppRoles.FEmployee,
                AppRoles.FUser,
                AppRoles.QManager,
                AppRoles.AManager,
                AppRoles.AEmployee,
                AppRoles.AUser,
                AppRoles.TManager,
                AppRoles.TEmployee,
                AppRoles.TSupport,
                AppRoles.TInsurance
            };
            // Runs on every cold start — one SELECT for the existing names
            // rather than one RoleExistsAsync round trip per role.
            var existing = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            foreach (var role in Roles)
            {
                if (!existing.Contains(role, StringComparer.OrdinalIgnoreCase))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}





