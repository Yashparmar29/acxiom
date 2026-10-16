using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;

namespace AcxiomCRM.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Customer Configurations
        builder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => e.CustomerCode).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Phone).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.AssignedTo);
            entity.HasIndex(e => e.CreatedDate);

            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(e => e.AssignedUser)
                  .WithMany()
                  .HasForeignKey(e => e.AssignedTo)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Lead Configurations
        builder.Entity<Lead>(entity =>
        {
            entity.HasIndex(e => e.LeadCode).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.AssignedTo);
            entity.HasIndex(e => e.CreatedDate);
            entity.HasIndex(e => e.IsDeleted);

            entity.Property(e => e.Source).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Priority).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(e => e.AssignedUser)
                  .WithMany()
                  .HasForeignKey(e => e.AssignedTo)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ConvertedCustomer)
                  .WithMany(c => c.Leads)
                  .HasForeignKey(e => e.ConvertedCustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Opportunity Configurations
        builder.Entity<Opportunity>(entity =>
        {
            entity.HasIndex(e => e.Stage);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.AssignedTo);
            entity.HasIndex(e => e.ExpectedCloseDate);
            entity.HasIndex(e => e.IsDeleted);

            entity.Property(e => e.Stage).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.Opportunities)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Lead)
                  .WithMany(l => l.Opportunities)
                  .HasForeignKey(e => e.LeadId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AssignedUser)
                  .WithMany()
                  .HasForeignKey(e => e.AssignedTo)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // FollowUp Configurations
        builder.Entity<FollowUp>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.FollowUpDate);
            entity.HasIndex(e => e.AssignedTo);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.LeadId);
            entity.HasIndex(e => e.OpportunityId);

            entity.Property(e => e.FollowUpType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.FollowUps)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Lead)
                  .WithMany(l => l.FollowUps)
                  .HasForeignKey(e => e.LeadId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Opportunity)
                  .WithMany(o => o.FollowUps)
                  .HasForeignKey(e => e.OpportunityId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AssignedUser)
                  .WithMany()
                  .HasForeignKey(e => e.AssignedTo)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Activity Configurations
        builder.Entity<Activity>(entity =>
        {
            entity.HasIndex(e => e.ActivityType);
            entity.HasIndex(e => e.ActivityDate);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.LeadId);
            entity.HasIndex(e => e.AssignedTo);

            entity.Property(e => e.ActivityType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.Activities)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Lead)
                  .WithMany(l => l.Activities)
                  .HasForeignKey(e => e.LeadId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AssignedUser)
                  .WithMany()
                  .HasForeignKey(e => e.AssignedTo)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // AuditLog Configurations
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.CreatedDate);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Module);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.RecordId);
        });
    }
}
