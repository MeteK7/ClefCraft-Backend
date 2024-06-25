using ClefCraft.Identity.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Identity.DbContext
{
    public class CleftCraftIdentityDbContext : IdentityDbContext<ApplicationUser>
    {
        public CleftCraftIdentityDbContext(DbContextOptions<CleftCraftIdentityDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(CleftCraftIdentityDbContext).Assembly);
        }
    }
}
