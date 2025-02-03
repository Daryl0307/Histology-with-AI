using Microsoft.EntityFrameworkCore;
using FYPProject.Models;

namespace FYPProject.Models
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options) { }

        // DbSet for all tables
        public DbSet<Account> Accounts { get; set; }
        public DbSet<TissueInfo> TissueInfo { get; set; }
        public DbSet<Photos> Photos { get; set; }
        public DbSet<Question> Question { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Photo table configuration
            modelBuilder.Entity<Photos>(entity =>
            {
                entity.HasKey(e => e.Photo_ID);
                entity.Property(e => e.Photo_ID).HasColumnName("Photo_ID");
                entity.Property(e => e.Photo_Description).HasColumnName("Photo_Description");
                entity.Property(e => e.Photo_URL).HasColumnName("Photo_URL");
                entity.Property(e => e.Tissue_ID).HasColumnName("Tissue_ID");
                entity.Ignore(e => e.PhotoFile);
                //entity.Property(e => e.Question_ID).HasColumnName("Question_ID");
            });

            // Question table configuration
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(e => e.QuestionId);
                entity.Property(e => e.QuestionId).HasColumnName("Question_ID");
                entity.Property(e => e.QuestionText).HasColumnName("Question_Text");
                entity.Property(e => e.QuestionMark).HasColumnName("Question_Marks");
                entity.Property(e => e.QuizId).HasColumnName("Quiz_ID");
                entity.Property(e => e.QuestionType).HasColumnName("Question_Type");
            });

            // TissueInfo table configuration
            modelBuilder.Entity<TissueInfo>(entity =>
            {
                entity.HasKey(e => e.TissueId);
                entity.Property(e => e.TissueId).HasColumnName("Tissue_ID");
                entity.Property(e => e.TissueName).HasColumnName("Tissue_Name");
                entity.Property(e => e.TissueDescription).HasColumnName("Tissue_Description");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
