using Integrator.Data;
using Integrator.Data.Entities;
using Integrator.Data.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Integrator.Logic
{
    [Obsolete("Использовался для изначальной разметки товаров. Сейчас неактуален ввиду механизма шаблонов и отдельного класса для поиска одинаковых товаров.")]
    public class ToponomyLogic
    {
        private readonly IntegratorDataContext dataContext;
        private readonly ILogger<ToponomyLogic> logger;

        public ToponomyLogic(IntegratorDataContext dataContext, ILogger<ToponomyLogic> logger)
        {
            this.logger = logger;
            this.dataContext = dataContext;
        }

        // Создание новых черновиков для категорий и брендов. В будущем нужно отказаться от этой сущности в пользу готового бренда со статусом IsAccepted или IsDraft
        public async Task CreateToponomyDrafts()
        {
            logger.LogInformation($"Process Cards: category drafts and brand drafts");

            var categoryCandidates = (await dataContext.Set<Card>()
                .AsNoTracking()
                .ToListAsync())
                .GroupBy(x => StringHelper.GetParentFolder(x.FolderPath, x.FolderName))
                .Select(x => new CategoryDraft(x.First()))
                .Where(x => x.IsAcceptable)
                .ToList();

            var categoryDraftsExisting = await dataContext.Set<CategoryDraft>()
                .AsNoTracking()
                .Select(x => x.Name)
                .ToListAsync();

            var categoryDraftsToAdd = categoryCandidates
                .Where(x => !categoryDraftsExisting.Contains(x.Name))
                .ToList();

            await dataContext.Set<CategoryDraft>().AddRangeAsync(categoryDraftsToAdd);

            // same as category
            var brandCandidates = (await dataContext.Set<Card>()
                .AsNoTracking()
                .ToListAsync())
                .GroupBy(x => StringHelper.GetParentFolder(x.FolderPath, x.FolderName))
                .Select(x => new BrandDraft(x.First()))
                .Where(x => x.IsAcceptable)
                .ToList();

            var brandDraftsExisting = await dataContext.Set<BrandDraft>()
                .AsNoTracking()
                .Select(x => x.Name)
                .ToListAsync();

            var brandDraftsToAdd = brandCandidates
                .Where(x => !brandDraftsExisting.Contains(x.Name))
                .ToList();

            await dataContext.Set<BrandDraft>().AddRangeAsync(brandDraftsToAdd);

            await dataContext.SaveChangesAsync();
        }


        // Размечение брендов и категорий для карточек исходя из путей и одинаковых текстов карточек.
        // TODO: Работу с одинаковыми текстами необходимо перенести в логику одинаковых товаров как отдельную ответственность.
        // TODO: Работу с именами из путей перенести в логику шаблонов
        public async Task MarkCardsWithToponomyItems()
        {
            var cards = await dataContext.Set<Card>()
                .Include(x => x.Detail)
                .ToListAsync();

            var categoryDrafts = await dataContext.Set<CategoryDraft>()
                .AsNoTracking()
                .Include(x => x.Card)
                .Where(x => x.CategoryId != null)
                .ToListAsync();

            var brandDrafts = await dataContext.Set<BrandDraft>()
                .AsNoTracking()
                .Include(x => x.Card)
                .Where(x => x.BrandId != null)
                .ToListAsync();

            var counterBrands = 0;
            var counterCategories = 0;

            #region Нужно переделать на логику шаблонов

            foreach (var card in cards)
            {
                if (card.Detail == null)
                {
                    card.Detail = new();
                }
            }

            foreach (var draft in categoryDrafts)
            {
                foreach (var card in cards.Where(x => new CategoryDraft(x).Name == draft.Name))
                {
                    card.Detail.CategoryId = draft.CategoryId;
                    counterCategories++;
                }
            }
            foreach (var draft in brandDrafts)
            {
                foreach (var card in cards.Where(x => new BrandDraft(x).Name == draft.Name))
                {
                    card.Detail.BrandId = draft.BrandId;
                    counterBrands++;
                }
            }

            #endregion

            #region Нужно перенести в логику одинаковых товаров

            foreach (var draft in brandDrafts)
            {
                // условие фильтрации может содержать ошибку, если текст будет пустым или стандартным и не будет содержать данных
                // TODO: если от данного кода не удастся отказаться, то бренд, как и категорию, нужно присваивать только товарам с некоторым рейтингом
                // TODO: или при наличии выставленного противоположного топономического элемента.
                foreach (var card in cards.Where(x => x.Detail.BrandId == null && x.TextFileContent == draft.Card.TextFileContent))
                {
                    card.Detail.BrandId = draft.BrandId;
                    counterBrands++;
                }
            }
            foreach (var draft in categoryDrafts)
            {
                // TODO: смотреть комментарий выше
                foreach (var card in cards.Where(x => x.Detail.CategoryId == null && x.TextFileContent == draft.Card.TextFileContent))
                {
                    card.Detail.CategoryId = draft.CategoryId;
                    counterCategories++;
                }
            }

            #endregion

            logger.LogInformation($"categories {counterCategories}, brands {counterBrands}");

            await dataContext.SaveChangesAsync();
        }
    }
}
