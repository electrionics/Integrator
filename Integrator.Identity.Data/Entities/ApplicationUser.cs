using Microsoft.AspNetCore.Identity;

namespace Integrator.Identity.Data.Entities;

// Add profile data for application users by adding properties to the User class
public class ApplicationUser : IdentityUser<string>
{
}