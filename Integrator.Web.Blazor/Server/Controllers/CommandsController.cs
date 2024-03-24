using Integrator.Logic;
using Microsoft.AspNetCore.Mvc;

namespace Integrator.Web.Blazor.Server.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CommandsController : ControllerBase
    {
        private readonly TemplateLogic templateLogic;
        private readonly ShopDirectoryLogic shopDirectoryLogic;
        private readonly TranslateLogic translateLogic;
        private readonly ReplacementLogic replacementLogic;
        private readonly ILogger<CommandsController> logger;
        private readonly SameCardsLogic sameCardsLogic;

        public CommandsController(ILogger<CommandsController> logger, TemplateLogic templateLogic, ShopDirectoryLogic shopDirectoryLogic, TranslateLogic translateLogic, ReplacementLogic replacementLogic, SameCardsLogic sameCardsLogic)
        {
            this.templateLogic = templateLogic;
            this.logger = logger;
            this.shopDirectoryLogic = shopDirectoryLogic;
            this.translateLogic = translateLogic;
            this.replacementLogic = replacementLogic;
            this.sameCardsLogic = sameCardsLogic;
        }

        #region Sync Shop Directories

        [HttpPost]
        public async Task SyncShopDirectories()
        {
            await shopDirectoryLogic.SyncShopsRoot();
        }

        #endregion

        #region Translate Card Texts

        [HttpPost]
        public async Task TranslateTexts()
        {
            await translateLogic.AddAllCardsNewTranslations();
        }

        #endregion

        #region Recalculate Card Detail Properties (brand, category, size, color, material, model)

        [HttpPost]
        public async Task RecalculateCards()
        {
            await templateLogic.ProcessCardsWithTemplates();

            await RecalculatePropsSearch();
            await RecalculatePropsApply();
        }

        private async Task RecalculatePropsSearch()
        {
            logger.LogInformation("Recalculate structure: search");
            await Task.CompletedTask;
        }

        private async Task RecalculatePropsApply()
        {
            logger.LogInformation("Recalculate structure: apply");
            await Task.CompletedTask;
        }

        #endregion

        #region Mark Same Cards

        [HttpPost]
        public async Task MarkSameCards()
        {
            await sameCardsLogic.MarkSameCards();
        }

        #endregion

        #region Recalculate Card Detail Texts (rus text)

        [HttpPost]
        public async Task RecalculateTexts()
        {
            await replacementLogic.ProcessCardsWithReplacements();
        }

        private async Task RecalculateTextSearch()
        {
            logger.LogInformation("Recalculate text search.");
            await Task.CompletedTask;
        }

        private async Task RecalculateTextApply()
        {
            logger.LogInformation("Recalculate text apply.");
            await Task.CompletedTask;
        }

        #endregion
    }
}
