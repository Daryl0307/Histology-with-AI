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
        public DbSet<HomeContent> HomeContent { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ✅ Fix for Photos table
            modelBuilder.Entity<Photos>(entity =>
            {
                entity.HasKey(e => e.Photo_ID);
                entity.Property(e => e.Photo_ID).HasColumnName("Photo_ID");
                entity.Property(e => e.Photo_Description).HasColumnName("Photo_Description");
                entity.Property(e => e.Photo_URL).HasColumnName("Photo_URL");
                entity.Property(p => p.Photo_Data).HasColumnType("VARBINARY(MAX)");

                entity.Ignore(p => p.PhotoFile); 

                // ✅ Ensure EF Core does NOT create unexpected relationships
                entity.HasOne<TissueInfo>()
                    .WithMany()
                    .HasForeignKey(p => p.Tissue_ID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Question>()
                    .WithMany()
                    .HasForeignKey(p => p.Question_ID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Quiz>()
                    .WithMany()
                    .HasForeignKey(p => p.Quiz_ID)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(e => e.QuestionId);
                entity.Property(e => e.QuestionId).HasColumnName("Question_ID");
                entity.Property(e => e.QuestionText).HasColumnName("Question_Text");
                entity.Property(e => e.QuestionMark).HasColumnName("Question_Marks");
                entity.Property(e => e.QuizId).HasColumnName("Quiz_ID");
                entity.Property(e => e.QuestionType).HasColumnName("Question_Type");
            });


            modelBuilder.Entity<TissueInfo>(entity =>
            {
                entity.HasKey(e => e.TissueId);
                entity.Property(e => e.TissueId).HasColumnName("Tissue_ID");
                entity.Property(e => e.TissueName).HasColumnName("Tissue_Name");
                entity.Property(e => e.TissueDescription).HasColumnName("Tissue_Description");
                entity.Ignore(e => e.PhotoFiles);
            });

            modelBuilder.Entity<HomeContent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Url).HasColumnName("Url").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Description).HasColumnName("Description").HasMaxLength(500).IsRequired();
                entity.Property(e => e.Photo_Data).HasColumnType("VARBINARY(MAX)"); // ✅ Store Image as BLOB
            });


            base.OnModelCreating(modelBuilder);
        }
    }
}
