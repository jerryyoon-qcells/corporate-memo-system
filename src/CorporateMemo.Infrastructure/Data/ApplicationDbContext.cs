using CorporateMemo.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CorporateMemo.Infrastructure.Data;

/// <summary>
/// The main Entity Framework Core database context for the Corporate Memo System.
/// Extends IdentityDbContext so that ASP.NET Core Identity tables (Users, Roles, etc.)
/// are created in the same database as our application tables.
///
/// This class:
/// 1. Defines DbSet properties for each entity we want to persist
/// 2. Configures how entities map to database tables (using Fluent API in OnModelCreating)
/// 3. Handles JSON column configuration for list properties (Tags, Recipients)
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>
    /// Initializes the DbContext with the provided options.
    /// Options (like the connection string) are injected via dependency injection from Program.cs.
    /// </summary>
    /// <param name="options">EF Core configuration options including the database provider and connection string.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the collection of all Memos in the database.
    /// EF Core uses this to generate the Memos table and all related queries.
    /// </summary>
    public DbSet<Memo> Memos => Set<Memo>();

    /// <summary>
    /// Gets or sets the collection of all ApprovalSteps in the database.
    /// Each row represents one approver's step for a specific memo.
    /// </summary>
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();

    /// <summary>
    /// Gets or sets the collection of all Attachments in the database.
    /// Each row represents one uploaded file linked to a memo.
    /// </summary>
    public DbSet<Attachment> Attachments => Set<Attachment>();

    /// <summary>
    /// Gets or sets the collection of all in-app Notifications in the database.
    /// </summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>
    /// Configures the entity-to-database mappings using EF Core's Fluent API.
    /// This is called automatically by EF Core when building the model.
    /// We use this method instead of data annotations on entities to keep domain classes clean.
    /// </summary>
    /// <param name="builder">The ModelBuilder used to configure entity shapes and relationships.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Call the base class to configure Identity tables (AspNetUsers, AspNetRoles, etc.)
        base.OnModelCreating(builder);

        // ================================================================
        // Configure ApplicationUser (extends IdentityUser)
        // ================================================================
        builder.Entity<ApplicationUser>(entity =>
        {
            // Add an index on DisplayName for faster user lookup in the approver search
            entity.HasIndex(u => u.DisplayName);
        });

        // ================================================================
        // Configure Memo entity
        // ================================================================
        builder.Entity<Memo>(entity =>
        {
            // Set maximum length for commonly queried string columns
            entity.Property(m => m.MemoNumber).HasMaxLength(50).IsRequired();
            entity.Property(m => m.Title).HasMaxLength(100).IsRequired();
            entity.Property(m => m.AuthorId).HasMaxLength(450).IsRequired();
            entity.Property(m => m.AuthorName).HasMaxLength(256).IsRequired();
            entity.Property(m => m.AuthorEmail).HasMaxLength(256);
            // M5 fix: Aligned with the requirement (1,000 characters) and the validator limit.
            // The previous value of 10,000 was 10x the specified requirement.
            entity.Property(m => m.Content).HasMaxLength(1000);

            // Create an index on MemoNumber for fast lookups by memo number
            entity.HasIndex(m => m.MemoNumber).IsUnique();

            // Create an index on AuthorId for fast "My Documents" queries
            entity.HasIndex(m => m.AuthorId);

            // Create an index on Status for fast filtering (e.g., only Published memos)
            entity.HasIndex(m => m.Status);

            // Store Tags as a JSON column in SQL Server
            // This lets us store a List<string> without a separate Tags table
            // The downside is that we cannot index or query individual tags as efficiently
            entity.Property(m => m.Tags)
                .HasConversion(
                    // When saving to DB: serialize the list to JSON string
                    tags => JsonSerializer.Serialize(tags, (JsonSerializerOptions?)null),
                    // When reading from DB: deserialize JSON string back to list
                    json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("nvarchar(max)");

            // Store ToRecipients as a JSON column
            entity.Property(m => m.ToRecipients)
                .HasConversion(
                    recipients => JsonSerializer.Serialize(recipients, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("nvarchar(max)");

            // Store CcRecipients as a JSON column
            entity.Property(m => m.CcRecipients)
                .HasConversion(
                    recipients => JsonSerializer.Serialize(recipients, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("nvarchar(max)");

            // Define the one-to-many relationship: one Memo has many ApprovalSteps
            entity.HasMany(m => m.ApprovalSteps)
                .WithOne(s => s.Memo)
                .HasForeignKey(s => s.MemoId)
                .OnDelete(DeleteBehavior.Cascade); // Deleting a memo deletes its approval steps

            // Define the one-to-many relationship: one Memo has many Attachments
            entity.HasMany(m => m.Attachments)
                .WithOne(a => a.Memo)
                .HasForeignKey(a => a.MemoId)
                .OnDelete(DeleteBehavior.Cascade); // Deleting a memo deletes its attachment records
        });

        // ================================================================
        // Configure ApprovalStep entity
        // ================================================================
        builder.Entity<ApprovalStep>(entity =>
        {
            entity.Property(s => s.ApproverId).HasMaxLength(450).IsRequired();
            entity.Property(s => s.ApproverName).HasMaxLength(256).IsRequired();
            entity.Property(s => s.ApproverEmail).HasMaxLength(256).IsRequired();
            entity.Property(s => s.Comment).HasMaxLength(1000);

            // Index on ApproverId + MemoId for fast "My Approvals" queries
            entity.HasIndex(s => new { s.ApproverId, s.MemoId });
        });

        // ================================================================
        // Configure Attachment entity
        // ================================================================
        builder.Entity<Attachment>(entity =>
        {
            entity.Property(a => a.FileName).HasMaxLength(256).IsRequired();
            entity.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(a => a.StoragePath).HasMaxLength(500).IsRequired();
        });

        // ================================================================
        // Configure Notification entity
        // ================================================================
        builder.Entity<Notification>(entity =>
        {
            entity.Property(n => n.UserId).HasMaxLength(450).IsRequired();
            entity.Property(n => n.Message).HasMaxLength(500).IsRequired();

            // Index on UserId for fast notification lookups
            entity.HasIndex(n => new { n.UserId, n.IsRead });
        });
    }
}
