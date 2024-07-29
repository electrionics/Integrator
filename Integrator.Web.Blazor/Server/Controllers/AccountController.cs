using FluentValidation;
using Integrator.Shared;
using Integrator.Web.Blazor.Shared.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Integrator.Web.Blazor.Server.Controllers
{
    [ApiController]
    //[Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;

        private readonly IValidator<RegisterModel> _registerValidator;

        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationConfig _config;

        public AccountController(SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            IValidator<RegisterModel> registerValidator,
            ILogger<AccountController> logger,
            ApplicationConfig config)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = (IUserEmailStore<ApplicationUser>)userStore;
            _registerValidator = registerValidator;
            _logger = logger;
            _config = config;
        }

        [HttpPost]
        [Route("/api/account/login")]
        public async Task<SignInResult> Login([FromBody] LoginModel model)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

            return result;
        }

        [HttpPost]
        [Route("/api/account/logout")]
        public async Task Logout()
        {
            await _signInManager.SignOutAsync();
        }

        [HttpPost]
        [Route("api/account/register")]
        public async Task<RegisterResult> Register([FromBody] RegisterModel model)
        {
            if (!_config.RegistrationEnabled)
            {
                return new RegisterResult
                {
                    Succeeded = false,
                    ErrorMessage = "Регистрация новых пользователей невозможна."
                };
            }

            var validationResult = await _registerValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                return new RegisterResult
                {
                    Succeeded = false,
                    ErrorMessage = validationResult.Errors.First().ErrorMessage
                };
            }

            var user = new ApplicationUser { Id = Guid.NewGuid().ToString() };

            await _userStore.SetUserNameAsync(user, model.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, model.Email, CancellationToken.None);
            try
            {
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return new RegisterResult
                    {
                        Succeeded = true
                    };
                }
                else
                {
                    return new RegisterResult
                    {
                        Succeeded = false,
                        ErrorMessage = result.Errors.First().Description,
                    };
                }
            }
            catch (Exception ex)
            {
            }


            throw new Exception();
        }
    }
}
