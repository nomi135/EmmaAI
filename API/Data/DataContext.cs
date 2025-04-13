using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class DataContext(DbContextOptions options) : IdentityDbContext<AppUser, AppRole, int, IdentityUserClaim<int>, AppUserRole, IdentityUserLogin<int>, 
        IdentityRoleClaim<int>, IdentityUserToken<int>>(options)
    {
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<UserChatHistory> ChatHistories { get; set; }

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
        }
    }
}
