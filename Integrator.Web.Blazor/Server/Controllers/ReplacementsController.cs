using Integrator.Data;
using Microsoft.AspNetCore.Mvc;

namespace Integrator.Web.Blazor.Server.Controllers
{
    [ApiController]
    [Route("[controller]/[action]/{id?}")]
    public class ReplacementsController : ControllerBase
    {
        private readonly ILogger<CardsController> logger;
        private readonly IntegratorDataContext dataContext;

        public ReplacementsController(ILogger<CardsController> logger, IntegratorDataContext dataContext)
        {
            this.logger = logger;
            this.dataContext = dataContext;
        }
    }
}
