using JobCandidates.Model;
using Microsoft.EntityFrameworkCore;

namespace JobCandidates
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Job> Jobs { get; set; }
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<AppUser> Users { get; set; }
        public DbSet<OtpCode> OtpCodes { get; set; }
        public DbSet<PendingRegistration> PendingRegistrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Job>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Status).HasMaxLength(20);
            });

            modelBuilder.Entity<Candidate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Skills).HasMaxLength(500);
            });

            modelBuilder.Entity<Application>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Candidate)
                    .WithMany(c => c.Applications)
                    .HasForeignKey(e => e.CandidateId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Job)
                    .WithMany(j => j.Applications)
                    .HasForeignKey(e => e.JobId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Interview>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(i => i.Application)
                    .WithMany(a => a.Interviews)
                    .HasForeignKey(i => i.ApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Gender).HasMaxLength(30);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50).HasDefaultValue("User");
            });

            modelBuilder.Entity<OtpCode>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.Email, e.Code });
            });

            modelBuilder.Entity<PendingRegistration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Gender).HasMaxLength(30);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50).HasDefaultValue("User");
                entity.Property(e => e.OtpCode).IsRequired().HasMaxLength(6);
            });
        }
    }
}