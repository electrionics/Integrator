using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Integrator.Identity.Data.Entities;

namespace Integrator.Identity.Data;

public class IdentityDataContext : IdentityDbContext<ApplicationUser, Role, string, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
{
    public IdentityDataContext(DbContextOptions<IdentityDataContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        #region Identity

        builder.Entity<ApplicationUser>().ToTable("_AspNetUser", "dbo");
        builder.Entity<Role>().ToTable("_AspNetRole", "dbo");
        builder.Entity<RoleClaim>().ToTable("_AspNetRoleClaim", "dbo");
        builder.Entity<UserClaim>().ToTable("_AspNetUserClaim", "dbo");
        builder.Entity<UserRole>().ToTable("_AspNetUserRole", "dbo");
        builder.Entity<UserLogin>().ToTable("_AspNetUserLogin", "dbo");
        builder.Entity<UserToken>().ToTable("_AspNetUserToken", "dbo");

        #endregion
    }
}
