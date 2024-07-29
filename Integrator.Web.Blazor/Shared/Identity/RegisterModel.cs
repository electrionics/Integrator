namespace Integrator.Web.Blazor.Shared.Identity
{
    public class RegisterModel
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public string ConfirmPassword { get; set; }

        public bool RememberMe { get; set; }
    }
}
