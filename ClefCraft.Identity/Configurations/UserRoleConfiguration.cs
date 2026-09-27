using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClefCraft.Identity.Configurations
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<string>>
    {
        public void Configure(EntityTypeBuilder<IdentityUserRole<string>> builder)
        {
            // No seeded role assignments: they belonged to the seeded users (see UserConfiguration).
            // The existing admin -> Administrator row is left in place by the DisableSeededAccounts
            // migration; new administrators are granted explicitly (see the deployment runbook).
        }
    }
}
