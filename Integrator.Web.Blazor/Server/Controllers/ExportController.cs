using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Integrator.Data;
using Integrator.Data.Entities;
using Integrator.Web.Blazor.Shared;
using Integrator.Web.Blazor.Shared.Export;
using Integrator.Shared.FluentImpex;


namespace Integrator.Web.Blazor.Server.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ExportController : ControllerBase
    {
        private readonly IntegratorDataContext dataContext;
        private readonly ILogger<ExportController> _logger;

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

        public ExportController(ILogger<ExportController> logger, IntegratorDataContext dataContext)
        {
            _logger = logger;
            this.dataContext = dataContext;
        }

        [HttpGet]
        public async Task<ExportFileViewModel?> GenerateExportFile()
        {
            try
            {
                #region Получение всех необходимых данных карточек
                var cards = await dataContext.Set<Card>().AsNoTracking()
                    .Include(x => x.Detail).ThenInclude(x => x.Brand)
                    .Include(x => x.Detail).ThenInclude(x => x.Category)
                    .Include(x => x.Detail).ThenInclude(x => x.Sizes).ThenInclude(x => x.Size)
                    .Include(x => x.Images)
                    .Include(x => x.Shop)
                    .Include(x => x.Similarities)
                    .ToDictionaryAsync(x => x.Id);
                #endregion

                var cardIdsToExclude = new List<int>();

                #region Заполнение деталей карточек из похожих карточек
                foreach (var cardItem in cards)
                {
                    var card = cardItem.Value;

                    if (card.Similarities.Any())
                    {
                        var similarCards = card.Similarities
                            .Select(x => x.SimilarCardId)
                            .Distinct()
                            .Select(x => cards[x])
                            .OrderByDescending(x=> x.Detail.Rating)
                            .ToList();

                        foreach(var similarCard in similarCards)
                        {
                            card.Detail.Brand ??= similarCard.Detail.Brand;
                            card.Detail.Category ??= similarCard.Detail.Category;

                            card.Detail.Model ??= similarCard.Detail.Model;
                            card.Detail.Color ??= similarCard.Detail.Color;
                            card.Detail.Price ??= similarCard.Detail.Price;
                            card.Detail.Material ??= similarCard.Detail.Material;

                            card.Detail.Sizes = card.Detail.Sizes.Any()
                                ? card.Detail.Sizes
                                : similarCard.Detail.Sizes;
                        }

                        cardIdsToExclude.AddRange(similarCards.Select(x => x.Id));
                    }
                }
                #endregion

                #region Создание списка карточек для экспорта
                var cardsToExport = cards
                    .Where(x => !cardIdsToExclude.Contains(x.Key))
                    .Select(x => new CardExportViewModel
                    {
                        CardExportId = x.Value.Id,
                        Active= true,
                        PreviewText = x.Value.Detail.ContentRus,

                        Brand = x.Value.Detail.Brand?.Name,
                        MainCategory = null,
                        SubCategory = x.Value.Detail.Category?.Name,

                        Model = x.Value.Detail.Model,
                        Price = x.Value.Detail.Price,
                        Color = x.Value.Detail.Color,
                        Material = x.Value.Detail.Material,

                        Sizes = string.Join(',', x.Value.Detail.Sizes.Select(y => y.Size.Value)),

                        Image1 = GetImageFullUrl(x.Value, 0),
                        Image2 = GetImageFullUrl(x.Value, 1),
                        Image3 = GetImageFullUrl(x.Value, 2),
                        Image4 = GetImageFullUrl(x.Value, 3),
                        Image5 = GetImageFullUrl(x.Value, 4),
                        Image6 = GetImageFullUrl(x.Value, 5),
                        Image7 = GetImageFullUrl(x.Value, 6),
                        Image8 = GetImageFullUrl(x.Value, 7),
                        Image9 = GetImageFullUrl(x.Value, 8),
                    })
                    .ToList();
                #endregion

                #region Создание файла с данными экспорта
                var fileName = "export_cards--" + DateTime.Now.ToString("yyyy_MM_dd-hh_mm_ss") + "--" + Guid.NewGuid().ToString().Substring(0, 4) + ".xlsx";
                var filePath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "exports", fileName);
                var metadata = new CardExportViewModelMetadata();
                var generator = new ExcelGenerator();

                var headerData = metadata.CreateHeader();
                var rowData = new List<List<string?>>();
                foreach (var card in cardsToExport)
                {
                    rowData.Add(metadata.CreateStrings(card));
                }

                var stream = generator.Generate("Экспорт", headerData, rowData);

                using (var fileStream = System.IO.File.Create(filePath))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fileStream);
                }
                #endregion

                var fileUrl = $"{HostBaseUrl}/exports/{fileName}";
                return new()
                {
                    Url = fileUrl
                };
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Ошибка при попытке сгенерировать файл экспорта товаров.");
                return null;
            }
        }

        private string? GetImageFullUrl(Card card, int index)
        {
            var images = card.Images;

            if (images.Count <= index)
                return null;

            var imageRelativePath = Path.Join("shops", card.Shop.Name, images[index].FolderPath, images[index].ImageFileName);
            var imageRelativeUrl = imageRelativePath.Replace("\\", "/");

            return $"{HostBaseUrl}/{imageRelativeUrl}";
        }
    }
}