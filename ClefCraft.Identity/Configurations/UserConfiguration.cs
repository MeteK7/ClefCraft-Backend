using ClefCraft.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClefCraft.Identity.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            // Users are deliberately not seeded with HasData: a seeded password is a credential
            // committed to source and applied to every database, production included. The local
            // development accounts (which the demo seed SQL references as owners) are created by
            // DevelopmentUserSeeder, with passwords from user-secrets.
        }
    }
}
