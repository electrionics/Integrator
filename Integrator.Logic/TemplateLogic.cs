using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Integrator.Data;
using Integrator.Data.Entities;
using Integrator.Shared;

namespace Integrator.Logic
{
    public class TemplateLogic
    {
        private readonly IntegratorDataContext dataContext;
        private readonly ILogger<TemplateLogic> logger;

        public TemplateLogic(IntegratorDataContext dataContext, ILogger<TemplateLogic> logger)
        {
            this.dataContext = dataContext;
            this.logger = logger;
        }

        public async Task ProcessCardsWithTemplates()
        {
            var templates = await dataContext.Set<Template>()
                .OrderBy(x => x.Order)
                .ToListAsync();
            var cards = await dataContext.Set<Card>()
                .Include(x => x.Translation)
                .Include(x => x.Detail).ThenInclude(x => x.Sizes)
                .Include(x => x.Detail).ThenInclude(x => x.Brand)
                .Include(x => x.Detail).ThenInclude(x => x.TemplateMatches)
                .ToListAsync();

            var sizes = await dataContext.Set<Size>()
                .ToListAsync();
            var brands = await dataContext.Set<Brand>()
                .ToListAsync();
            var categories = await dataContext.Set<Category>()
                .ToListAsync();

            var referencesCreationContext = new Dictionary<(TemplateApplyField applyField, string newValue), object> ();
            var executablesAfterCreation = new List<(Card card, Template template, Func<string> getValueFunc)>();

            // обработка шаблонами карточек существующими значениями категорий, брендов, размеров
            var counter = 0;
            foreach (var template in templates)
            {
                foreach (var card in cards)
                {
                    var processingResult = ProcessCardWithTemplate(card, template, sizes, brands, categories);
                    if (processingResult.matched)
                    {
                        counter++;
#pragma warning disable CS8604 // Possible null reference argument.
                        AddMatchingToContext(card, template, processingResult.value);
#pragma warning restore CS8604 // Possible null reference argument.
                    }

                    if (processingResult.newValue != null)
                    {
                        var getValueFunc = ProcessNotMatchedValue(card, template, processingResult.newValue, referencesCreationContext);
                        if (getValueFunc != null)
                        {
                            executablesAfterCreation.Add((card, template, getValueFunc));
                        }
                    }

                    if (counter > 0 && counter % 100 == 0)
                    {
                        counter++;
                        await dataContext.SaveChangesAsync();
                        logger.LogInformation($"Already matched: {counter}");
                    }
                }
            }

            await dataContext.SaveChangesAsync();

            // создание необходимых новых категорий и брендов
            foreach (var referenceCreation in referencesCreationContext)
            {
                switch (referenceCreation.Key.applyField)
                {
                    case TemplateApplyField.Brand:
                        dataContext.Set<Brand>().Add((Brand)referenceCreation.Value);
                        break;
                    case TemplateApplyField.Category:
                        dataContext.Set<Category>().Add((Category)referenceCreation.Value);
                        break;
                    case TemplateApplyField.Size:
                        dataContext.Set<Size>().Add((Size)referenceCreation.Value);
                        break;
                }
            }

            try
            {
                await dataContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Не удалось добавить новые сущности для структурирования карточек.");
            }
            return;
            // getValueFunc нужно тестировать, похоже что пока она не работает: возвращает нулевые Id новых сущностей

            sizes = await dataContext.Set<Size>()
                .ToListAsync();
            brands = await dataContext.Set<Brand>()
                .ToListAsync();
            categories = await dataContext.Set<Category>()
                .ToListAsync();

            // обработка шаблонами карточек отложенными новыми значениями категорий, брендов, размеров
            counter = 0;
            foreach (var executable in executablesAfterCreation)
            {
                counter++;
                AddMatchingToContext(executable.card, executable.template, executable.getValueFunc()); // функция получения значения идентификатора(ов) из сохраненных топономических единиц (категорий, брендов, размеров)

                if (counter > 0 && counter % 100 == 0)
                {
                    counter++;
                    await dataContext.SaveChangesAsync();
                    logger.LogInformation($"Already matched: {counter}");
                }
            }

            await dataContext.SaveChangesAsync();
        }


        #region Вспомогательные методы

        private (bool matched, string? value, string? newValue) ProcessCardWithTemplate(Card card, Template template, List<Size> allSizes, List<Brand> allBrands, List<Category> allCategories)
        {
            bool isSuccess;
            string? applyValue = null;

            #region Поиск по тексту и получение применяемого значения

            if (template.IsRegexp)
            {
                Regex templateRegex = new(template.SearchValue, RegexOptions.IgnoreCase);
                var match = templateRegex.Match(template.GetCardText(card));

                if (template.ApplyValue == null)
                {
                    if (match.Groups.Count >= 2)
                    {
                        applyValue = match.Groups[1].Value;
                    }
                    else if (match.Success)
                    {
                        logger.LogWarning(
                                $"Карточка {card.Id}. " +
                                $"Шаблон {template.Id}. " +
                                $"Регулярное '{template.SearchValue}' выражение не содержит групп.");
                    }
                }
                else
                {
                    applyValue = template.ApplyValue;
                }

                isSuccess = match.Success;
            }
            else
            {
                applyValue = template.ApplyValue;
                isSuccess = template
                    .GetCardText(card)
                    .Contains(template.SearchValue, StringComparison.InvariantCultureIgnoreCase);
            }

            #endregion

            if (isSuccess)
            {
                switch (template.ApplyField)
                {
                    case TemplateApplyField.Price:
                        if (decimal.TryParse(applyValue, out var price)) // 1 group must be here TODO: debug
                        {
                            return (true, price.ToString(), null);
                        }
                        else
                        {
                            logger.LogWarning(
                                $"Карточка {card.Id}. " +
                                $"Шаблон {template.Id}. " +
                                $"Значение '{applyValue}' не подходит для цены.");

                            return (false, null, null);
                        }
                    case TemplateApplyField.Brand:
                        var applyBrand = allBrands.FirstOrDefault(x => x.Name.Equals(template.ApplyValue, StringComparison.InvariantCultureIgnoreCase));

                        if (applyBrand == null)
                        {
                            logger.LogWarning(
                                $"Карточка {card.Id}. " +
                                $"Шаблон {template.Id}. " +
                                $"Бренд с именем '{applyValue}' не найден в базе");
                            return (false, null, applyValue);
                        }

                        return (true, applyBrand.Id.ToString(), null);
                    case TemplateApplyField.Category:
                        var applyCategory = allCategories.FirstOrDefault(x => x.Name.Equals(template.ApplyValue, StringComparison.OrdinalIgnoreCase));

                        if (applyCategory == null)
                        {
                            logger.LogWarning(
                                $"Карточка {card.Id}. " +
                                $"Шаблон {template.Id}. " +
                                $"Категория с именем '{applyValue}' не найдена в базе");
                            return (false, null, applyValue);
                        }

                        return (true, applyCategory.Id.ToString(), null);
                    case TemplateApplyField.Size:
                        List<string> templateSetSizeValues = applyValue
                            .Split(',')
                            .ToList(); // semicolon-seperated values must be here

                        var applySizes = allSizes
                            .Where(x => templateSetSizeValues.Contains(x.Value, StringComparer.OrdinalIgnoreCase))
                            .Select(x => new CardDetailSize
                            {
                                SizeId = x.Id,
                                CardId = card.Id
                            })
                            .ToList();

                        if (applySizes.Count == 0)
                        {
                            return (false, null, applyValue);
                        }

                        var valueSizes = string.Join(',', applySizes.Select(x => x.SizeId));
                        if (applySizes.Count != templateSetSizeValues.Count)
                        {
                            var newSizes = templateSetSizeValues.Where(x => !allSizes.Select(y => y.Value).Contains(x, StringComparer.OrdinalIgnoreCase));
                            var newValueSizes = string.Join(',', newSizes);
                            return (true, valueSizes, newValueSizes);
                        }

                        return (true, valueSizes, null);
                    case TemplateApplyField.Color:
                    case TemplateApplyField.Material:
                    case TemplateApplyField.Model:
                        return (true, applyValue, null);
                    default:
                        return (false, null, null);
                }
            }

            return (false, null, null);
        }

        private Func<string> ProcessNotMatchedValue(Card card, Template template, string newValue, Dictionary<(TemplateApplyField, string), object> context)
        {
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
#pragma warning disable CS8601 // Possible null reference assignment.
            switch (template.ApplyField)
            {
                case TemplateApplyField.Brand:
                    
                    if (!context.ContainsKey((template.ApplyField, newValue)))
                    {
                        var newBrand = new Brand() { Name = newValue, DisplayName = newValue };
                        context.Add((template.ApplyField, newValue), newBrand);
                    }

                    var brand = (Brand)context[(template.ApplyField, newValue)];
                    return brand.Id.ToString;
                case TemplateApplyField.Category:
                    if (!context.ContainsKey((template.ApplyField, newValue)))
                    {
                        var newCategory = new Category() { Name = newValue, DisplayName = newValue };
                        context.Add((template.ApplyField, newValue), newCategory);
                    }

                    var category = (Category)context[(template.ApplyField, newValue)];
                    return category.Id.ToString;
                case TemplateApplyField.Size:
                    var newSizeValues = newValue.Split(',').ToList();
                    var newSizes = new List<Size>();

                    foreach(var newSizeValue in newSizeValues)
                    {
                        if (!context.ContainsKey((template.ApplyField, newSizeValue)))
                        {
                            var newSize = new Size() { Value = newSizeValue };
                            context.Add((template.ApplyField, newSizeValue), newSize);
                        }

                        newSizes.Add((Size)context[(template.ApplyField, newSizeValue)]);
                    }

                    return () => string.Join(",", newSizes.Select(x => x.Id));
                default:
                    return null;
            }
#pragma warning restore CS8601 // Possible null reference assignment.
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
        }

        private void SetTemplateMatchingValue(Card card, Template template, string? matchingValue)
        {
            if (!string.IsNullOrEmpty(matchingValue))
            {
                switch (template.ApplyField)
                {
                    case TemplateApplyField.Price:

                        var price = decimal.Parse(matchingValue);
                        logger.LogWarningIf(card.Detail.Price != null && card.Detail.Price != price,
                            $"Карточка {card.Id}. " +
                            $"Шаблон {template.Id}. " +
                            $"Значение цены {card.Detail.Price} перезаписано новым значением {price}.");

                        card.Detail.Price = price;
                        break;
                    case TemplateApplyField.Brand:
                        var newBrandId = int.Parse(matchingValue);
                        logger.LogWarningIf(card.Detail.BrandId != null
                            && newBrandId != card.Detail.BrandId,
                                $"Карточка {card.Id}. " +
                                $"Шаблон {template.Id}. " +
                                $"Бренд '{card.Detail.BrandId}:{card.Detail.Brand?.Name ?? string.Empty}' перезаписан новым значением '{newBrandId}:{template.ApplyValue}'");

                        card.Detail.BrandId = newBrandId;
                        break;
                    case TemplateApplyField.Category:
                        var newCategoryId = int.Parse(matchingValue);
                        logger.LogWarningIf(card.Detail.CategoryId != null
                            && newCategoryId != card.Detail.CategoryId,
                                $"Карточка {card.Id}. " +
                                $"Шаблон {template.Id}. " +
                                $"Категория '{card.Detail.CategoryId}:{card.Detail.Category?.Name ?? string.Empty}' перезаписана новым значением '{newCategoryId}:{template.ApplyValue}'");

                        card.Detail.CategoryId = newCategoryId;
                        break;
                    case TemplateApplyField.Color:
                        logger.LogWarningIf(card.Detail.Color != matchingValue,
                                $"Карточка {card.Id}. " +
                                $"Шаблон {template.Id}. " +
                                $"Цвет '{card.Detail.Color}' перезаписан новым значением '{matchingValue}'");

                        card.Detail.Color = matchingValue;
                        break;
                    case TemplateApplyField.Model:
                        logger.LogWarningIf(card.Detail.Model != matchingValue,
                                $"Карточка {card.Id}. " +
                                $"Шаблон {template.Id}. " +
                                $"Модель '{card.Detail.Model}' перезаписана новым значением '{matchingValue}'");

                        card.Detail.Model = matchingValue;
                        break;
                    case TemplateApplyField.Material:
                        logger.LogWarningIf(card.Detail.Material != matchingValue,
                                $"Карточка {card.Id}. " +
                                $"Шаблон {template.Id}. " +
                                $"Материал '{card.Detail.Material}' перезаписан новым значением '{matchingValue}'");

                        card.Detail.Material = matchingValue;
                        break;
                    case TemplateApplyField.Size:
                        try
                        {
                            var newSizeIds = matchingValue
                                .Split(',')
                                .Select(int.Parse)
                                .ToList();
                            var newSizes = newSizeIds.Select(x => new CardDetailSize
                            {
                                SizeId = x,
                                CardId = card.Id
                            });

                            var beforeCount = card.Detail.Sizes.Count;
                            var newCount = newSizeIds.Count;

                            card.Detail.Sizes = card.Detail.Sizes
                                .UnionBy(newSizes, x => x.SizeId)
                                .ToList();

                            var afterCount = card.Detail.Sizes.Count;

                            logger.LogWarningIf(beforeCount + newCount != afterCount,
                                    $"Карточка {card.Id}. " +
                                    $"Шаблон {template.Id}. " +
                                    $"Размеры пересекаются! Количество размеров до изменения: {beforeCount}, новых: {newCount}, после изменения: {afterCount}");
                        }
                        catch(Exception ex)
                        {
                            logger.LogError(ex, 
                                    $"Карточка {card.Id}. " +
                                    $"Шаблон {template.Id}. " +
                                    $"Ошибка при установке размеров. Значение для установки: {matchingValue}.");
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private CardDetailTemplateMatch? AddMatchingToContext(Card card, Template template, string value)
        {
            var matching = new CardDetailTemplateMatch()
            {
                CardDetailId = card.Id,
                TemplateId = template.Id,

                Value = value
            };

            if (card.Detail == null)
            {
                card.Detail = new() { TemplateMatches = new() };
            }

            if (!card.Detail.TemplateMatches.Any(x =>
                x.TemplateId == matching.TemplateId))
            {
                SetTemplateMatchingValue(card, template, matching.Value);
                dataContext.Set<CardDetailTemplateMatch>().Add(matching);
                return matching;
            }
            else
            {
                logger.LogInformation("Matching already processed.");
                return null;
            }
        }

        #endregion
    }
}