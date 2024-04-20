﻿using CsvHelper;
using CsvHelper.Configuration;
using Integrator.Data;
using Integrator.Data.Entities;
using Integrator.Logic.Export;
using Integrator.Shared;
using Integrator.Shared.FluentImpex;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RussianTransliteration;
using System.Globalization;

namespace Integrator.Logic
{
    public class ExportLogic
    {
        private readonly IntegratorDataContext dataContext;
        private readonly ApplicationConfig appConfig;
        private readonly ILogger<ExportLogic> _logger;

        public ExportLogic(IntegratorDataContext dataContext, ApplicationConfig appConfig, ILogger<ExportLogic> logger)
        {
            this.dataContext = dataContext;
            this.appConfig = appConfig;
            _logger = logger;
        }

        public async Task<string> GenerateExportFile(string hostBaseUrl, ExportFileType fileType)
        {
            try
            {
                _logger.LogInformation("Экспорт: операция запущена.");

                #region Получение всех необходимых данных карточек
                var cards = await dataContext.Set<Card>().AsNoTracking()
                    .Include(x => x.Detail).ThenInclude(x => x.Brand)
                    .Include(x => x.Detail).ThenInclude(x => x.Category)
                    .Include(x => x.Detail).ThenInclude(x => x.Sizes).ThenInclude(x => x.Size)
                    .Include(x => x.Images)
                    .Include(x => x.Shop)
                    .Include(x => x.Similarities)
                    .Where(x => x.Detail != null && x.Images.Any())
                    .ToDictionaryAsync(x => x.Id);
                #endregion

                _logger.LogInformation("Экспорт: данные получены");

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
                            .OrderByDescending(x => x.Detail.Rating)
                            .ToList();

                        foreach (var similarCard in similarCards)
                        {
                            card.Detail.Brand ??= similarCard.Detail.Brand;

                            card.Detail.Category ??= similarCard.Detail.Category;
                            if (string.Equals(card.Detail.Category?.Name, "Обувь", StringComparison.InvariantCultureIgnoreCase) &&
                                similarCard.Detail.Category != null)
                            {
                                card.Detail.Category = similarCard.Detail.Category; //TODO: make low priority merge category with name "Обувь"
                            }

                            card.Detail.Color = ReplaceNullWithNew(card.Detail.Color, similarCard.Detail.Color);
                            card.Detail.Price = GetNewPrice(card.Detail.Price, similarCard.Detail.Price);

                            card.Detail.Sizes = card.Detail.Sizes.Any()
                                ? card.Detail.Sizes
                                : similarCard.Detail.Sizes;
                        }

                        cardIdsToExclude.AddRange(similarCards.Select(x => x.Id));
                    }
                }
                #endregion

                _logger.LogInformation("Экспорт: слияние одинаковых товаров произведено успешно.");

                var now = DateTime.Now.Date;

                #region Создание списка карточек для экспорта
                var cardsToExport = cards
                    .Where(x => !cardIdsToExclude.Contains(x.Key))
                    .Select(x =>
                    {
                        var item = new CardExportViewModel
                        {
                            CardExportId = x.Value.Id,
                            ActiveFromDate = now,
                            Price = x.Value.Detail.Price,
                            Brand = x.Value.Detail.Brand?.DisplayName,
                            BrandCode = x.Value.Detail.Brand?.Id,

                            Code = GetCode(x.Value.Shop, x.Value.Detail),

                            SubCategory = x.Value.Detail.Category?.DisplayName,
                            Color = x.Value.Detail.Color,

                            Sizes = GetSizes(x.Value.Detail.Sizes),

                            Image1 = GetImageFullUrl(x.Value, 0, hostBaseUrl),
                            Image2 = GetImageFullUrl(x.Value, 1, hostBaseUrl),
                            Image3 = GetImageFullUrl(x.Value, 2, hostBaseUrl),
                            Image4 = GetImageFullUrl(x.Value, 3, hostBaseUrl),
                            Image5 = GetImageFullUrl(x.Value, 4, hostBaseUrl),
                            Image6 = GetImageFullUrl(x.Value, 5, hostBaseUrl),
                            Image7 = GetImageFullUrl(x.Value, 6, hostBaseUrl),
                            Image8 = GetImageFullUrl(x.Value, 7, hostBaseUrl),
                            Image9 = GetImageFullUrl(x.Value, 8, hostBaseUrl),
                        };

                        item.Model = GetModelName(x.Value.Detail.Category, x.Value.Detail.Brand, item.Code);
                        item.Url = Transliterate(item.Model).Replace(" ", "_");

                        return item;
                    })
                    .ToList();
                #endregion

                var goodCards = cardsToExport.Count(x => !string.IsNullOrEmpty(x.Brand) && !string.IsNullOrEmpty(x.SubCategory));

                _logger.LogInformation("Экспорт: модели для формирования строк получены.");

                #region Создание файла с данными экспорта
                string extension = fileType switch
                {
                    ExportFileType.Excel => ".xlsx",
                    ExportFileType.Csv => ".csv",
                    _ => throw new NotImplementedException("Тип файла экспорта не реализован."),
                };

                var fileName = "export_cards--" + DateTime.Now.ToString("yyyy_MM_dd-hh_mm_ss") + "--" + Guid.NewGuid().ToString().Substring(0, 4) + extension;
                var filePath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, appConfig.RootFolder, "exports", fileName);

                switch (fileType)
                {
                    case ExportFileType.Excel:
                        var metadata = new CardExportViewModelMetadata();
                        var generator = new ExcelGenerator();

                        var headerData = metadata.CreateHeader();
                        var rowData = new List<List<string?>>();
                        foreach (var card in cardsToExport)
                        {
                            rowData.Add(metadata.CreateStrings(card));
                        }

                        var stream = generator.Generate("Экспорт", headerData, rowData);

                        using (var fileStream = File.Create(filePath))
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            stream.CopyTo(fileStream);
                        }
                        break;
                    case ExportFileType.Csv:
                        var config = CsvConfiguration.FromAttributes<CardExportViewModel>();

                        using (var fileStream = File.Create(filePath))
                        using (var writer = new StreamWriter(fileStream))
                        using (var csv = new CsvWriter(writer, config))
                        {
                            csv.WriteHeader<CardExportViewModel>();
                            csv.NextRecord();
                            csv.WriteRecords(cardsToExport);
                        }
                        break;
                    default: 
                        throw new NotImplementedException("Тип файла экспорта не реализован.");
                }
                
                #endregion

                _logger.LogInformation($"Экспорт: файл '{fileName}' сгенерирован успешно.");

                var fileUrl = $"{hostBaseUrl}/exports/{fileName}";
                return fileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при попытке сгенерировать файл экспорта товаров.");
                return null;
            }
        }

        #region Слияние одинаковых товаров

        private static decimal? GetNewPrice(decimal? sourceCardPrice, decimal? similarCardPrice)
        {
            if (sourceCardPrice == null)
            {
                return similarCardPrice;
            }

            if (similarCardPrice == null)
            {
                return sourceCardPrice;
            }

            return Math.Min(sourceCardPrice.Value, similarCardPrice.Value);
        }

        private static string? ReplaceNullWithNew(string? oldValue, string? newValue)
        {
            return oldValue ?? newValue;
        }

        #endregion

        #region Получение данных для экспорта

        /// <summary>
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <param name="brand"></param>
        /// <param name="code">Артикул</param>
        /// <returns></returns>
        private static string GetModelName(Category? category, Brand? brand, string code)
        {
            var name = category == null
                ? string.Empty
                : category.DisplayName + " ";

            name += brand == null
                ? string.Empty
                : brand.DisplayName + " ";

            name += code;

            return name;
        }

        private static string GetCode(Shop shop, CardDetail cardDetail)
        {
            return $"IN-{cardDetail.CardId:00000}-{shop.Id:000}";
        }

        private static string Transliterate(string value)
        {
            return RussianTransliterator.GetTransliteration(value);
        }

        private static string GetSizes(List<CardDetailSize> sizes)
        {
            return string.Join(',', sizes.Select(y => y.Size.Value));
        }

        private static string? GetImageFullUrl(Card card, int index, string hostBaseUrl)
        {
            var images = card.Images;

            if (images.Count <= index)
                return null;

            var imageRelativePath = Path.Join("shops", card.Shop.Name, images[index].FolderPath, images[index].ImageFileName);
            var imageRelativeUrl = imageRelativePath.Replace("\\", "/");

            return $"{hostBaseUrl}/{imageRelativeUrl}";
        }

        #endregion
    }
}
