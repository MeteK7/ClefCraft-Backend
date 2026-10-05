using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClefCraft.Identity.Configurations
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<string>>
    {
        public void Configure(EntityTypeBuilder<IdentityUserRole<string>> builder)
        {
            // No seeded role assignments: no users are seeded either (see UserConfiguration).
            // DevelopmentUserSeeder grants the local admin its role; new administrators are granted
            // explicitly (see the deployment runbook).
        }
    }
}
