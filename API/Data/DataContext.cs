using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace API.Data
{
    public class DataContext(DbContextOptions options) : IdentityDbContext<AppUser, AppRole, int, IdentityUserClaim<int>, AppUserRole, IdentityUserLogin<int>, 
        IdentityRoleClaim<int>, IdentityUserToken<int>>(options)
    {
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<UserChatHistory> ChatHistories { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<SurveyForm> SurveyForms { get; set; }
        public DbSet<SurveyFormData> SurveyFormData { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<AppUser>()
                .HasMany(ur => ur.UserRoles)
                .WithOne(u => u.User)
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();

            builder.Entity<AppRole>()
                .HasMany(ur => ur.UserRoles)
                .WithOne(u => u.Role)
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired();

            builder.Entity<UserChatHistory>()
                .HasOne(ch => ch.AppUser)   // ChatHistory has one AppUser
                .WithMany(u => u.ChatHistories) // AppUser has many ChatHistories (you need to add this collection in AppUser class)
                .HasForeignKey(ch => ch.AppUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Reminder>()
                .HasOne(r => r.AppUser)   // Reminder has one AppUser
                .WithMany(u => u.Reminders) // AppUser has many Reminders (you need to add this collection in AppUser class)
                .HasForeignKey(r => r.AppUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<SurveyForm>()
                .HasMany(sd => sd.SurveyFormDetails) // SurveyForm has many SurveyFormDetails
                .WithOne(s => s.SurveyForm) // SurveyFormDetails has one SurveyForm
                .HasForeignKey(sd => sd.SurveyFormId)
                .OnDelete(DeleteBehavior.Cascade); // delete SurveyFormDetails also

            builder.Entity<SurveyFormData>()
                .HasOne(d => d.User)
                .WithMany(u => u.SurveyFormData) // AppUser has many SurveyFormData
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SurveyFormData>()
                .HasOne(d => d.SurveyFormDetail)
                .WithMany(f => f.SurveyFormData) // SurveyFormDetail has many SurveyFormData
                .HasForeignKey(d => d.SurveyFormDetailId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
