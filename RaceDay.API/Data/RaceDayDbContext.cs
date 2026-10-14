using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

public partial class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Enrolment> Enrolments { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<Participant> Participants { get; set; }

    public virtual DbSet<Result> Results { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__19093A2B0ED04CDE");

            entity.ToTable("Category");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Distance).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.EntryFee).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.EventId).HasColumnName("EventID");

            entity.HasOne(d => d.Event).WithMany(p => p.Categories)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Category_Event");
        });

        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.HasKey(e => e.EnrolmentId).HasName("PK__Enrolmen__5C0E5FEF7E773F96");

            entity.ToTable("Enrolment");

            entity.HasIndex(e => new { e.ParticipantId, e.CategoryId }, "UQ_Enrolment_Participant_Category").IsUnique();

            entity.Property(e => e.EnrolmentId).HasColumnName("EnrolmentID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.EnrolmentDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ParticipantId).HasColumnName("ParticipantID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Category).WithMany(p => p.Enrolments)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Enrolment_Category");

            entity.HasOne(d => d.Participant).WithMany(p => p.Enrolments)
                .HasForeignKey(d => d.ParticipantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Enrolment_Participant");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__Event__7944C87065841D57");

            entity.ToTable("Event");

            entity.Property(e => e.EventId).HasColumnName("EventID");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Distance).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.EventName).HasMaxLength(150);
            entity.Property(e => e.EventType).HasMaxLength(20);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.OrganiserId).HasColumnName("OrganiserID");

            entity.HasOne(d => d.Organiser).WithMany(p => p.Events)
                .HasForeignKey(d => d.OrganiserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Event_Organiser");
        });

        modelBuilder.Entity<Participant>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Particip__1788CCACE2E4E79B");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("UserID");

            entity.HasOne(d => d.User).WithOne(p => p.Participant)
                .HasForeignKey<Participant>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Participants_User");
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasKey(e => e.ResultId).HasName("PK__Result__97690228C72F9B6B");

            entity.ToTable("Result");

            entity.HasIndex(e => e.EnrolmentId, "UQ__Result__5C0E5FEED546B4D5").IsUnique();

            entity.Property(e => e.ResultId).HasColumnName("ResultID");
            entity.Property(e => e.EnrolmentId).HasColumnName("EnrolmentID");
            entity.Property(e => e.RecordedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ResultStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Completed");

            entity.HasOne(d => d.Enrolment).WithOne(p => p.Result)
                .HasForeignKey<Result>(d => d.EnrolmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Result_Enrolment");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CCAC57C8826B");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ__User__A9D10534F526846B").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Role).HasMaxLength(20);
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId).HasName("PK__UserProf__290C88843BC6C67A");

            entity.ToTable("UserProfile");

            entity.HasIndex(e => e.UserId, "UQ__UserProf__1788CCAD3C96B645").IsUnique();

            entity.Property(e => e.ProfileId).HasColumnName("ProfileID");
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.EmergencyContact).HasMaxLength(100);
            entity.Property(e => e.ProfileImageUrl).HasMaxLength(500);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithOne(p => p.UserProfile)
                .HasForeignKey<UserProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserProfile_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
