using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Timeouts;

using Integrator.Web.Blazor.Shared;
using Integrator.Logic;
using Integrator.Logic.Export;

namespace Integrator.Web.Blazor.Server.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ExportController : ControllerBase
    {
        #region HostBaseUrl

        private string? hostBaseUrl;
        private string HostBaseUrl
        {
            get
            {
                if (hostBaseUrl == null)
                {
                    hostBaseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
                }

                return hostBaseUrl;
            }
        }

        #endregion

        private readonly ExportLogic _exportLogic;
        private readonly ILogger<ExportController> _logger;

        public ExportController(ExportLogic exportLogic, ILogger<ExportController> logger)
        {
            _exportLogic = exportLogic;
            _logger = logger;
        }

        [HttpGet]
        [RequestTimeout(ServerConstants.LongRunningPolicyName)]
        public async Task<ExportFileViewModel> GenerateExportFile()
        {
            var url = await _exportLogic.GenerateExportFile(HostBaseUrl, ExportFileType.Csv);

            return new()
            {
                Url = string.IsNullOrEmpty(url) ? null : url
            };
        }

        [HttpGet]
        public async Task<ExportItemViewModel[]> GetExports()
        {
            var data = await _exportLogic.GetExports();

            var result = data.Select(x => new ExportItemViewModel
            {
                 ExternalFileId = x.ExternalFileId,
                 FileName = x.FileName,
                 DateTimeGenerated = x.Created,
                 IsSelected = x.IsSelected,
                 FileReport = new ExportItemReport
                 {
                     NonPriced = x.NonPriced,
                     ExcludedAsRepeatableCount = x.ExcludedAsRepeatableCount,
                     NoBrandAndCategoryCount = x.NoBrandAndCategoryCount
                 }
            }).ToArray();

            return result;
        }

        [HttpPost]
        public async Task<bool> PerformSelection(ExportFileViewModel model)
        {
            try
            {
                return await _exportLogic.PerformSelection(model.ExternalFileId!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при попытке выбрать файл: {model.ExternalFileId!}");

                return false;
            }
        }

        [HttpPost]
        public async Task<bool> SignalToBitrix(ExportFileViewModel model)
        {
            try
            {
                return await _exportLogic.SendCommandToRecipient(model.ExternalFileId!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при попытке отправить сигнал Битриксу: {model.ExternalFileId!}");

                return false;
            }
        }
    }
}