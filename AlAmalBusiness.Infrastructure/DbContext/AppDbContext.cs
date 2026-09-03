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

        modelBuilder.Entity<LeadCall>()
            .HasOne(c => c.Actor)
            .WithMany()
            .HasForeignKey(c => c.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

    }
    }

 