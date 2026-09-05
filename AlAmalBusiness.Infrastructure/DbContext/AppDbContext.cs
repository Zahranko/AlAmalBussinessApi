using AlAmalBusiness.Domain.Models;
using AlAmalBusiness.Domain.Models.CRM;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AlAmalBusiness.DbContext.Infrastructure;

public class AppDbContext : IdentityDbContext<User>
{

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
      public DbSet<Departments> Departments { get; set; }
      public DbSet<Lead> Leads { get; set; }
      public DbSet<Doctors> Doctors { get; set; }
      public DbSet<Procedures> Procedures { get; set; }
      public DbSet<ReferalSource> Referals { get; set; }
      public DbSet<ClosedReason> ClosedReasons { get; set; }
      public DbSet<LeadHistory> LeadHistories { get; set; }
      public DbSet<LeadCall> LeadCalls { get; set; }
      public DbSet<RefreshToken> RefreshTokens { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Department)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Lead>()
            .HasOne(l => l.CreatedBy)
            .WithMany(u => u.CreatedLeads)
            .HasForeignKey(l => l.CreatedById)
            .OnDelete(DeleteBehavior.Restrict); 

        
        modelBuilder.Entity<Lead>()
            .HasOne(l => l.ClaimedBy)
            .WithMany(u => u.ClaimedLeads)
            .HasForeignKey(l => l.ClaimedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Lead>()
            .HasOne(l => l.ClosedReason)
            .WithMany(r => r.Leads)
            .HasForeignKey(l => l.ClosedReasonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Every list page filters on Status (open vs. completed) and sorts by
        // CreatedDate DESC with OFFSET/FETCH, and the queue/KPI counts range
        // on CreatedDate — without these both the COUNT and the page were a
        // full scan + sort of Leads on every request.
        modelBuilder.Entity<Lead>().HasIndex(l => new { l.Status, l.CreatedDate });
        modelBuilder.Entity<Lead>().HasIndex(l => l.CreatedDate);

        // Bounded lengths so these stop being nvarchar(max) LOB columns (read
        // off-row, un-indexable). Sized generously above anything real.
        modelBuilder.Entity<Lead>().Property(l => l.Name).HasMaxLength(200);
        modelBuilder.Entity<Lead>().Property(l => l.NickName).HasMaxLength(100);
        modelBuilder.Entity<Lead>().Property(l => l.PhoneNum).HasMaxLength(32);
        modelBuilder.Entity<Lead>().Property(l => l.CountryKey).HasMaxLength(10);

        // Dashboard "successes" KPI filters LeadHistories on Type + ResultingStatus.
        modelBuilder.Entity<LeadHistory>().HasIndex(h => new { h.Type, h.ResultingStatus });

        // Calendar feed reads each lead's latest call (ORDER BY CreatedAt DESC
        // per LeadId); the plain FK index only covers LeadId.
        modelBuilder.Entity<LeadCall>().HasIndex(c => new { c.LeadId, c.CreatedAt });

        modelBuilder.Entity<LeadHistory>()
            .HasOne(h => h.Lead)
            .WithMany()
            .HasForeignKey(h => h.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LeadHistory>()
            .HasOne(h => h.Actor)
            .WithMany()
            .HasForeignKey(h => h.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LeadHistory>()
            .HasOne(h => h.Doctor)
            .WithMany()
            .HasForeignKey(h => h.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LeadHistory>()
            .HasOne(h => h.ClosedReason)
            .WithMany()
            .HasForeignKey(h => h.ClosedReasonId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LeadCall>()
            .HasOne(c => c.Lead)
            .WithMany()
            .HasForeignKey(c => c.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            // Deleting a user takes their sessions with them; nothing else
            // references these rows.
            .OnDelete(DeleteBehavior.Cascade);

        // Every refresh looks a token up by its hash, so this is the one
        // index that matters. Unique because a hash collision would mean two
        // sessions sharing a credential.
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash)
            .IsUnique();

        // Supports "revoke everything for this user" and the expiry sweep.
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => new { t.UserId, t.ExpiresAt });

        modelBuilder.Entity<LeadCall>()
            .HasOne(c => c.Actor)
            .WithMany()
            .HasForeignKey(c => c.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

    }
    }

 