﻿using System.Diagnostics;

using Microsoft.Extensions.Logging;
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
        #region Google Translate Client

        private static TranslationServiceClient _client;
        private static object _clientSync = new object();

        private TranslationServiceClient Client
        {
            get
            {
                if (applicationConfig == null)
                {
                    throw new InvalidOperationException("Can't access client before set application config with settings for it.");
                }

                if (_client == null)
                {
                    lock (_clientSync)
                    {
                        var builder = new TranslationServiceClientBuilder
                        {
                            GrpcAdapter = RestGrpcAdapter.Default,
                        };
                        if (applicationConfig.GoogleCredentialsPath is not null)
                        {
                            builder.CredentialsPath = applicationConfig.GoogleCredentialsPath;
                        }

                        _client = builder.Build();
                    }
                }

                return _client;
            }
        }

        #endregion


        private readonly IntegratorDataContext dataContext;
        private readonly ILogger<TranslateLogic> logger;
        private readonly ApplicationConfig applicationConfig;
        

        public TranslateLogic(IntegratorDataContext dataContext, ApplicationConfig applicationConfig, ILogger<TranslateLogic> logger)
        {
            this.logger = logger;
            this.dataContext = dataContext;
            this.applicationConfig = applicationConfig;
        }

        public async Task AddAllCardsNewTranslations()
        {
            var shops = await dataContext.Set<Shop>().ToListAsync();

            foreach (var shop in shops)
            {
                await AddShopCardNewTranslations(shop.Name);
            }
        }

        #region Structural Methods

        // Переводит все тексты карточек товаров и относительные пути к папке карточки для выбранного магазина.
        // Карточки, которые ранее были переведены, не переводятся повторно.
        // Карточки с одинаковым текстом переводятся один раз.
        private async Task AddShopCardNewTranslations(string shopName)
        {
            logger.LogInformation($"Translate shop: {shopName}");

            #region Prepare data and environment for translation

            var shop = await dataContext.Set<Shop>().FirstAsync(x => x.Name == shopName);
            var cards = await dataContext.Set<Card>().Where(x => x.ShopId == shop.Id && x.Translation == null).ToListAsync();

            //TranslationServiceClient client = TranslationServiceClient.Create();

            // переменная texts - хранилище неповторяющихся текстов карточек,
            // переменная translations - хранилище сущностей, содержащих уже переведенные тексты с привязкой к идентификатору карточки
            // переменная translationsBatch - текущая партия переведенных сущностей, подлежащая по достижении определенного размера сохранению
            var allTranslations = new Dictionary<int, CardTranslation>();
            var texts = new Dictionary<string, int>();
            var translationsBatch = new List<CardTranslation>();
            const int translationsBatchSize = 100;

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

                if (allTranslations.TryGetValue(texts[card.TextFileContent], out var existingTranslation))
                {
                    cardTranslation.ContentEng = existingTranslation.ContentEng ?? string.Empty;
                    cardTranslation.ContentRus = existingTranslation.ContentRus ?? string.Empty;

                    try
                    {
                        var eng = await TranslateAsync(Client, "en-US", card.FolderPath);
                        var rus = await TranslateAsync(Client, "ru-RU", card.FolderPath);

                        // пути переводим всегда, пренебрегаем экономией на них
                        cardTranslation.TitleEng = eng.path;
                        cardTranslation.TitleRus = rus.path;

                        allTranslations.Add(card.Id, cardTranslation);
                        translationsBatch.Add(cardTranslation);
                    }
                    catch (RpcException ex) when (ex.StatusCode == StatusCode.ResourceExhausted)
                    {
                        await ClearTranslationsBatch(translationsBatch, sw); // сохраняем то, что уже переведено, избегая потерь при превышении установленных для бесплатного перевода квот
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Unpredictable error while translating path.");
                        await ClearTranslationsBatch(translationsBatch, sw);
                        return;
                    }
                }
                else
                {
                    try
                    {
                        var eng = await TranslateAsync(Client, "en-US", card.FolderPath, card.TextFileContent);
                        var rus = await TranslateAsync(Client, "ru-RU", card.FolderPath, card.TextFileContent);

                        cardTranslation.TitleEng = eng.path;
                        cardTranslation.ContentEng = eng.text ?? string.Empty;
                        cardTranslation.TitleRus = rus.path;
                        cardTranslation.ContentRus = rus.text ?? string.Empty;

                        allTranslations.Add(card.Id, cardTranslation);
                        translationsBatch.Add(cardTranslation);
                    }
                    catch (RpcException ex) when (ex.StatusCode == StatusCode.ResourceExhausted)
                    {
                        await ClearTranslationsBatch(translationsBatch, sw);
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Unpredictable error while translating text and path.");
                        await ClearTranslationsBatch(translationsBatch, sw);
                        return;
                    }
                }
                
                if (translationsBatch.Count == translationsBatchSize)
                {
                    await ClearTranslationsBatch(translationsBatch, sw);
                }
            }

            logger.LogInformation("No errors during translation");
            await ClearTranslationsBatch(translationsBatch, sw);
        }
        // @exceptionshandled

        private async Task<bool> ClearTranslationsBatch(List<CardTranslation> batch, Stopwatch sw)
        {
            sw.Stop();
            logger.LogInformation($"Time spent: {sw.Elapsed} translations: {2 * batch.Select(x => x.ContentEng).Distinct().Count()}");

            dataContext.Set<CardTranslation>().AddRange(batch);

            try
            {
                await dataContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Oops, something wrong with adding translations to database. Please re-run current operation. Exception: {ex.GetType()} message: {ex.Message}");

                return false;
            }
            finally
            {
                batch.Clear();
            }
        }
        // @exceptionshandled

        #endregion


        #region Helper Methods

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
