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
            // committed to source and applied to every database, production included. The two
            // accounts that used to be seeded still exist (seed SQL references them as owners) but
            // had their passwords cleared by the DisableSeededAccounts migration. Local development
            // passwords come from user-secrets via DevelopmentUserSeeder.
        }
    }
}
