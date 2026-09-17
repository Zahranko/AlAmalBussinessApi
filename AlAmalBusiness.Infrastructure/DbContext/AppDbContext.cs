using AlAmalBusiness.Domain.Models;
using AlAmalBusiness.Domain.Models.Appointments;
using AlAmalBusiness.Domain.Models.CRM;
using AlAmalBusiness.Domain.Models.Feedback;
using AlAmalBusiness.Domain.Models.Questionnaires;
using AlAmalBusiness.Domain.Models.Tickets;
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
      public DbSet<DeletedLead> DeletedLeads { get; set; }
      public DbSet<RefreshToken> RefreshTokens { get; set; }
      public DbSet<PatientFeedback> Feedbacks { get; set; }
      public DbSet<FeedbackHistory> FeedbackHistories { get; set; }
      public DbSet<Questionnaire> Questionnaires { get; set; }
      public DbSet<QuestionnaireQuestion> QuestionnaireQuestions { get; set; }
      public DbSet<QuestionnaireSubmission> QuestionnaireSubmissions { get; set; }
      public DbSet<QuestionnaireAnswer> QuestionnaireAnswers { get; set; }
      public DbSet<QuestionnaireReportRun> QuestionnaireReportRuns { get; set; }
      public DbSet<AppointmentRequest> AppointmentRequests { get; set; }
      public DbSet<AppointmentHistory> AppointmentHistories { get; set; }
      public DbSet<AppointmentReferralSource> AppointmentReferralSources { get; set; }
      public DbSet<Ticket> Tickets { get; set; }
      public DbSet<TicketHistory> TicketHistories { get; set; }
      public DbSet<TicketCategory> TicketCategories { get; set; }
      public DbSet<TicketProcedure> TicketProcedures { get; set; }
      public DbSet<TicketReason> TicketReasons { get; set; }


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

        // Admin soft delete: a deleted lead disappears from every Leads query
        // (queues, calendar, dashboards, KPIs, exports) without touching each
        // one. The recycle-bin queries in LeadRepo opt out with
        // IgnoreQueryFilters(). LeadHistories/LeadCalls are read by LeadId, so
        // anything aggregating them across leads must exclude deleted leads
        // itself (see LeadHistoryRepo.SucceededInRange).
        modelBuilder.Entity<Lead>().HasQueryFilter(l => !l.IsDeleted);

        modelBuilder.Entity<DeletedLead>()
            .HasOne(d => d.Lead)
            .WithMany()
            .HasForeignKey(d => d.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DeletedLead>()
            .HasOne(d => d.DeletedBy)
            .WithMany()
            .HasForeignKey(d => d.DeletedById)
            .OnDelete(DeleteBehavior.Restrict);

        // One recycle-bin row per deleted lead; the list sorts by DeletedAt.
        modelBuilder.Entity<DeletedLead>().HasIndex(d => d.LeadId).IsUnique();
        modelBuilder.Entity<DeletedLead>().HasIndex(d => d.DeletedAt);

        // ---------- Feedback ----------

        // Restrict, so retiring a department (IsActive = false) is the only
        // way it ever leaves the picker — old patient messages keep pointing
        // at the department they were actually sent to.
        modelBuilder.Entity<PatientFeedback>()
            .HasOne(f => f.Department)
            .WithMany()
            .HasForeignKey(f => f.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PatientFeedback>()
            .HasOne(f => f.AssignedTo)
            .WithMany()
            .HasForeignKey(f => f.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);

        // The reference number is the patient's only handle on their message,
        // and staff look messages up by it — so it is both unique and indexed.
        modelBuilder.Entity<PatientFeedback>()
            .HasIndex(f => f.ReferenceNumber)
            .IsUnique();

        // The inbox always filters on the caller's department and sorts by
        // CreatedDate DESC with OFFSET/FETCH; without this both the COUNT and
        // the page are a full scan + sort of Feedbacks on every request.
        modelBuilder.Entity<PatientFeedback>().HasIndex(f => new { f.DepartmentId, f.CreatedDate });
        modelBuilder.Entity<PatientFeedback>().HasIndex(f => new { f.Status, f.CreatedDate });

        // Bounded lengths so these stop being nvarchar(max) LOB columns (read
        // off-row, un-indexable) — the search predicate runs over three of
        // them. Details is deliberately left unbounded: it is free text the
        // patient wrote, and no list query selects it.
        modelBuilder.Entity<PatientFeedback>().Property(f => f.ReferenceNumber).HasMaxLength(32);
        modelBuilder.Entity<PatientFeedback>().Property(f => f.FirstName).HasMaxLength(60);
        modelBuilder.Entity<PatientFeedback>().Property(f => f.LastName).HasMaxLength(60);
        modelBuilder.Entity<PatientFeedback>().Property(f => f.PhoneCountryCode).HasMaxLength(6);
        modelBuilder.Entity<PatientFeedback>().Property(f => f.PhoneNumber).HasMaxLength(32);
        modelBuilder.Entity<PatientFeedback>().Property(f => f.SubmittedFromIp).HasMaxLength(64);
        modelBuilder.Entity<PatientFeedback>().Property(f => f.UserAgent).HasMaxLength(400);

        modelBuilder.Entity<FeedbackHistory>()
            .HasOne(h => h.Feedback)
            .WithMany()
            .HasForeignKey(h => h.FeedbackId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FeedbackHistory>()
            .HasOne(h => h.Actor)
            .WithMany()
            .HasForeignKey(h => h.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        // The timeline is always read as one message's entries in order.
        modelBuilder.Entity<FeedbackHistory>().HasIndex(h => new { h.FeedbackId, h.CreatedAt });

        modelBuilder.Entity<FeedbackHistory>().Property(h => h.FromDepartmentName).HasMaxLength(200);
        modelBuilder.Entity<FeedbackHistory>().Property(h => h.ToDepartmentName).HasMaxLength(200);

        // ---------- Questionnaires ----------

        // Restrict, like PatientFeedback: a department is retired with
        // IsActive, never deleted out from under its questionnaires.
        modelBuilder.Entity<Questionnaire>()
            .HasOne(q => q.Department)
            .WithMany()
            .HasForeignKey(q => q.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Questionnaire>()
            .HasOne(q => q.CreatedBy)
            .WithMany()
            .HasForeignKey(q => q.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // The slug is the public page's address, so it must be unique; the
        // public lookup reads by it on every page open.
        modelBuilder.Entity<Questionnaire>().HasIndex(q => q.Slug).IsUnique();
        modelBuilder.Entity<Questionnaire>().Property(q => q.Slug).HasMaxLength(60);
        modelBuilder.Entity<Questionnaire>().Property(q => q.Title).HasMaxLength(200);
        modelBuilder.Entity<Questionnaire>().Property(q => q.Description).HasMaxLength(1000);

        modelBuilder.Entity<QuestionnaireQuestion>()
            .HasOne(x => x.Questionnaire)
            .WithMany(q => q.Questions)
            .HasForeignKey(x => x.QuestionnaireId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuestionnaireQuestion>().Property(x => x.Text).HasMaxLength(500);

        modelBuilder.Entity<QuestionnaireSubmission>()
            .HasOne(s => s.Questionnaire)
            .WithMany()
            .HasForeignKey(s => s.QuestionnaireId)
            .OnDelete(DeleteBehavior.Cascade);

        // Every results query is "this questionnaire's submissions in a period".
        modelBuilder.Entity<QuestionnaireSubmission>().HasIndex(s => new { s.QuestionnaireId, s.CreatedDate });
        modelBuilder.Entity<QuestionnaireSubmission>().Property(s => s.SubmittedFromIp).HasMaxLength(64);
        modelBuilder.Entity<QuestionnaireSubmission>().Property(s => s.UserAgent).HasMaxLength(400);
        modelBuilder.Entity<QuestionnaireSubmission>().Property(s => s.Name).HasMaxLength(100);
        modelBuilder.Entity<QuestionnaireSubmission>().Property(s => s.PhoneNumber).HasMaxLength(20);
        modelBuilder.Entity<QuestionnaireSubmission>().Property(s => s.Notes).HasMaxLength(2000);

        // The monthly report's once-per-month guarantee rests on this index.
        modelBuilder.Entity<QuestionnaireReportRun>().HasIndex(r => new { r.Year, r.Month }).IsUnique();

        modelBuilder.Entity<QuestionnaireAnswer>()
            .HasOne(a => a.Submission)
            .WithMany(s => s.Answers)
            .HasForeignKey(a => a.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, not Cascade: a questionnaire already cascades to answers
        // through its submissions, and SQL Server refuses a second cascade
        // path. Nothing needs it anyway — an answered question is archived,
        // never deleted.
        modelBuilder.Entity<QuestionnaireAnswer>()
            .HasOne(a => a.Question)
            .WithMany(x => x.Answers)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        // ---------- Appointments ----------

        // Restrict, like PatientFeedback: a department is retired with
        // IsActive, never deleted out from under the requests sent to it.
        modelBuilder.Entity<AppointmentRequest>()
            .HasOne(a => a.Department)
            .WithMany()
            .HasForeignKey(a => a.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppointmentRequest>()
            .HasOne(a => a.ReferralSource)
            .WithMany()
            .HasForeignKey(a => a.ReferralSourceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppointmentRequest>()
            .HasOne(a => a.AssignedTo)
            .WithMany()
            .HasForeignKey(a => a.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);

        // The inbox always filters on the caller's department and sorts by
        // CreatedDate DESC with OFFSET/FETCH; without these both the COUNT
        // and the page are a full scan + sort on every request. Same shape as
        // the Feedbacks indexes, for the same reason.
        modelBuilder.Entity<AppointmentRequest>().HasIndex(a => new { a.DepartmentId, a.CreatedDate });
        modelBuilder.Entity<AppointmentRequest>().HasIndex(a => new { a.Status, a.CreatedDate });
        modelBuilder.Entity<AppointmentRequest>().HasIndex(a => a.CreatedDate);

        // Bounded lengths so these stop being nvarchar(max) LOB columns (read
        // off-row, un-indexable) — the search predicate runs over two of
        // them. Details is deliberately left unbounded at the column level
        // beyond its 2000-char cap: it is free text the patient wrote, and no
        // list query selects it.
        modelBuilder.Entity<AppointmentRequest>().Property(a => a.FullName).HasMaxLength(120);
        modelBuilder.Entity<AppointmentRequest>().Property(a => a.PhoneCountryCode).HasMaxLength(6);
        modelBuilder.Entity<AppointmentRequest>().Property(a => a.PhoneNumber).HasMaxLength(32);
        modelBuilder.Entity<AppointmentRequest>().Property(a => a.Details).HasMaxLength(2000);
        modelBuilder.Entity<AppointmentRequest>().Property(a => a.SubmittedFromIp).HasMaxLength(64);
        modelBuilder.Entity<AppointmentRequest>().Property(a => a.UserAgent).HasMaxLength(400);

        modelBuilder.Entity<AppointmentHistory>()
            .HasOne(h => h.Appointment)
            .WithMany()
            .HasForeignKey(h => h.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AppointmentHistory>()
            .HasOne(h => h.Actor)
            .WithMany()
            .HasForeignKey(h => h.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        // The timeline is always read as one request's entries in order.
        modelBuilder.Entity<AppointmentHistory>().HasIndex(h => new { h.AppointmentId, h.CreatedAt });

        modelBuilder.Entity<AppointmentHistory>().Property(h => h.FromDepartmentName).HasMaxLength(200);
        modelBuilder.Entity<AppointmentHistory>().Property(h => h.ToDepartmentName).HasMaxLength(200);

        // A unique name backs the service's own duplicate check against a race.
        modelBuilder.Entity<AppointmentReferralSource>().Property(r => r.Name).HasMaxLength(200);
        modelBuilder.Entity<AppointmentReferralSource>().HasIndex(r => r.Name).IsUnique();

        // ---------- Tickets ----------

        // Restrict on every lookup, user and department: each is retired with
        // IsActive (or disabled, for a user), never deleted out from under the
        // tickets that point at it.
        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Procedure)
            .WithMany()
            .HasForeignKey(t => t.ProcedureId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Reason)
            .WithMany()
            .HasForeignKey(t => t.ReasonId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.CreatedBy)
            .WithMany()
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.AssignedTo)
            .WithMany()
            .HasForeignKey(t => t.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Department)
            .WithMany()
            .HasForeignKey(t => t.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // One per list: the queue filters on Status and sorts by CreatedDate,
        // created-by-me filters on the creator, and the queue is split on
        // PaymentMethod (the insurance desk's side vs. everyone else's).
        // Without them each COUNT and page is a full scan + sort, same
        // reasoning as the appointment indexes.
        modelBuilder.Entity<Ticket>().HasIndex(t => new { t.Status, t.CreatedDate });
        modelBuilder.Entity<Ticket>().HasIndex(t => new { t.CreatedById, t.CreatedDate });
        modelBuilder.Entity<Ticket>().HasIndex(t => new { t.AssignedToId, t.CreatedDate });
        modelBuilder.Entity<Ticket>().HasIndex(t => new { t.PaymentMethod, t.Status, t.CreatedDate });

        // Bounded so the searched Title isn't an nvarchar(max) LOB; the caps
        // match what CreateTicketDTO accepts.
        modelBuilder.Entity<Ticket>().Property(t => t.Title).HasMaxLength(200);
        modelBuilder.Entity<Ticket>().Property(t => t.Description).HasMaxLength(4000);
        modelBuilder.Entity<Ticket>().Property(t => t.PatientId).HasMaxLength(64);
        modelBuilder.Entity<Ticket>().Property(t => t.SourceUrl).HasMaxLength(1000);
        modelBuilder.Entity<Ticket>().Property(t => t.Resolution).HasMaxLength(2000);

        modelBuilder.Entity<TicketHistory>()
            .HasOne(h => h.Ticket)
            .WithMany()
            .HasForeignKey(h => h.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TicketHistory>()
            .HasOne(h => h.Actor)
            .WithMany()
            .HasForeignKey(h => h.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        // The timeline is always read as one ticket's entries in order.
        modelBuilder.Entity<TicketHistory>().HasIndex(h => new { h.TicketId, h.CreatedAt });
        modelBuilder.Entity<TicketHistory>().Property(h => h.Note).HasMaxLength(2000);

        // Unique names back the services' own duplicate checks against a race.
        modelBuilder.Entity<TicketCategory>().Property(c => c.Name).HasMaxLength(200);
        modelBuilder.Entity<TicketCategory>().HasIndex(c => c.Name).IsUnique();
        modelBuilder.Entity<TicketProcedure>().Property(p => p.Name).HasMaxLength(200);
        modelBuilder.Entity<TicketProcedure>().HasIndex(p => p.Name).IsUnique();

        modelBuilder.Entity<TicketReason>()
            .HasOne(r => r.Procedure)
            .WithMany()
            .HasForeignKey(r => r.ProcedureId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TicketReason>().Property(r => r.Name).HasMaxLength(200);
        // Per procedure — see TicketReason.
        modelBuilder.Entity<TicketReason>().HasIndex(r => new { r.ProcedureId, r.Name }).IsUnique();

    }
    }

 