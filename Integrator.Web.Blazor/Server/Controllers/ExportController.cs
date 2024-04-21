using Microsoft.AspNetCore.Mvc;
using Integrator.Web.Blazor.Shared;
using Integrator.Logic;
using Integrator.Logic.Export;
using Microsoft.AspNetCore.Http.Timeouts;


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

        public ExportController(ExportLogic exportLogic)
        {
            _exportLogic = exportLogic;
        }

        [HttpGet]
        [DisableRequestTimeout]
        public async Task<ExportFileViewModel?> GenerateExportFile()
        {
            var url = await _exportLogic.GenerateExportFile(HostBaseUrl, ExportFileType.Csv);
            if (string.IsNullOrEmpty(url))
                return null;

            return new()
            {
                Url = url
            };
        }
    }
}