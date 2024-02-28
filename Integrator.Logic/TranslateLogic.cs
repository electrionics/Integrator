using System.Diagnostics;

using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Google.Cloud.Translate.V3;
using Google.Api.Gax.Grpc.Rest;
using Google.Api.Gax.ResourceNames;
using Grpc.Core;

using Integrator.Data;
using Integrator.Data.Entities;
using Integrator.Shared;

namespace Integrator.Logic
{
    public class TranslateLogic
    {
        private readonly IntegratorDataContext dataContext;
        private readonly ILogger logger;

        public TranslateLogic(IntegratorDataContext dataContext, ILogger logger)
        {
            this.logger = logger;
            this.dataContext = dataContext;
        }

        public async Task TranslateDatabase()
        {
            var shops = await dataContext.Set<Shop>().ToListAsync();

            foreach (var shop in shops)
            {
                await TranslateShop(shop.Name);
            }
        }

        // Переводит все тексты карточек товаров и относительные пути к папке карточки для выбранного магазина.
        // Карточки, которые ранее были переведены, не переводятся повторно.
        // Карточки с одинаковым текстом переводятся один раз.
        public async Task TranslateShop(string shopName)
        {
            logger.WriteLine($"Translate shop: {shopName}");

            #region Prepare to translate

            var shop = await dataContext.Set<Shop>().FirstAsync(x => x.Name == shopName);
            var cards = await dataContext.Set<Card>().Where(x => x.ShopId == shop.Id && x.CardTranslation == null).ToListAsync();

            var client = new TranslationServiceClientBuilder
            {
                GrpcAdapter = RestGrpcAdapter.Default,
            }.Build();
            //TranslationServiceClient client = TranslationServiceClient.Create();

            var translations = new Dictionary<int, CardTranslation>();
            var texts = new Dictionary<string, int>();

            foreach (var card in cards)
            {
                texts.TryAdd(card.TextFileContent, card.Id);
            }

            #endregion

            var sw = Stopwatch.StartNew();

            foreach (var card in cards)
            {
                var cardTranslation = new CardTranslation
                {
                    CardId = card.Id,
                };

                // same text already translated for current batch
                if (translations.TryGetValue(texts[card.TextFileContent], out var existingTranslation))
                {
                    cardTranslation.ContentEng = existingTranslation.ContentEng ?? string.Empty;
                    cardTranslation.ContentRus = existingTranslation.ContentRus ?? string.Empty;

                    try
                    {
                        var eng = await TranslateAsync(client, "en-US", card.FolderPath);
                        var rus = await TranslateAsync(client, "ru-RU", card.FolderPath);

                        cardTranslation.TitleEng = eng.path;
                        cardTranslation.TitleRus = rus.path;

                        translations.Add(card.Id, cardTranslation);
                    }
                    catch (RpcException ex) when (ex.StatusCode == StatusCode.ResourceExhausted)
                    {
                        await SaveAsync(translations, sw);
                        return;
                    }
                    catch (Exception)
                    {
                        await SaveAsync(translations, sw);
                        throw;
                    }
                }
                else
                {
                    try
                    {
                        var eng = await TranslateAsync(client, "en-US", card.FolderPath, card.TextFileContent);
                        var rus = await TranslateAsync(client, "ru-RU", card.FolderPath, card.TextFileContent);

                        cardTranslation.TitleEng = eng.path;
                        cardTranslation.ContentEng = eng.text ?? string.Empty;
                        cardTranslation.TitleRus = rus.path;
                        cardTranslation.ContentRus = rus.text ?? string.Empty;

                        translations.Add(card.Id, cardTranslation);
                    }
                    catch (RpcException ex) when (ex.StatusCode == StatusCode.ResourceExhausted)
                    {
                        await SaveAsync(translations, sw);
                        return;
                    }
                    catch (Exception)
                    {
                        await SaveAsync(translations, sw);
                        throw;
                    }
                }
            }

            logger.WriteLine("No errors during translation!!!");
            await SaveAsync(translations, sw);
        }


        #region Helper Methods

        private async Task SaveAsync(Dictionary<int, CardTranslation> translations, Stopwatch sw)
        {
            sw.Stop();
            logger.WriteLine($"{sw.Elapsed} translations: {2 * translations.Select(x => x.Value.ContentEng).Distinct().Count()}");
            dataContext.Set<CardTranslation>().AddRange(translations.Select(x => x.Value));
            await dataContext.SaveChangesAsync();
        }

        private static async Task<(string path, string text)> TranslateAsync(TranslationServiceClient client, string languageCode, string path, string? sourceText = null)
        {
            TranslateTextRequest request;

            if (sourceText.IsNullOrEmpty())
            {
                request = new()
                {
                    Contents = { path }
                };
            }
            else if (path.IsNullOrEmpty())
            {
                request = new()
                {
                    Contents = { sourceText }
                };
            }
            else
            {
                request = new()
                {
                    Contents = { path, sourceText }
                };
            }

            request.TargetLanguageCode = languageCode;
            request.Parent = new ProjectName("integrator-oleg-v1").ToString();

            var response = await client.TranslateTextAsync(request);

            (string path, string text) result;

            if (sourceText.IsNullOrEmpty())
            {
                result = new()
                {
                    path = response.Translations[0].TranslatedText
                }; // response.Translations will have one entry, because request.Contents has one entry.
            }
            else if (path.IsNullOrEmpty())
            {
                result = new()
                {
                    text = response.Translations[0].TranslatedText
                };
            }
            else
            {
                result = new()
                {
                    path = response.Translations[0].TranslatedText,
                    text = response.Translations[1].TranslatedText
                };
            }

            return result;
        }

        #endregion
    }
}
