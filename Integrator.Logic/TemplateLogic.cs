using Integrator.Data;
using Integrator.Data.Entities;
using Integrator.Shared;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Integrator.Logic
{
    public class TemplateLogic
    {
        private readonly IntegratorDataContext dataContext;
        private readonly ILogger logger;

        public TemplateLogic(IntegratorDataContext dataContext, ILogger logger)
        {
            this.dataContext = dataContext;
            this.logger = logger;
        }

        public async Task ProcessDatabaseWithTemplates()
        {
            var templates = await dataContext.Set<Template>()
                .OrderBy(x => x.Order)
                .ToListAsync();

            var cards = await dataContext.Set<Card>()
                .Include(x => x.CardTranslation)
                .Include(x => x.CardDetail).ThenInclude(x => x.CardDetailSizes)
                .Include(x => x.CardDetail).ThenInclude(x => x.Brand)
                .Include(x => x.CardDetail).ThenInclude(x => x.TemplateMatches)
                .ToListAsync();

            var sizes = await dataContext.Set<Size>()
                .ToListAsync();

            var brands = await dataContext.Set<Brand>()
                .ToListAsync();

            var counter = 0;
            foreach (var template in templates)
            {
                foreach (var card in cards)
                {
                    var processingResult = ProcessCardWithTemplate(card, template, sizes, brands);
                    if (processingResult.matched)
                    {
                        counter++;
                        var matching = new CardDetailTemplateMatch()
                        {
                            CardDetailId = card.Id,
                            TemplateId = template.Id,

                            Value = processingResult.value
                        };

                        if (!card.CardDetail.TemplateMatches.Any(x =>
                            x.TemplateId == matching.TemplateId))
                        {
                            SetTemplateMatchingValue(card, template, matching.Value);
                            dataContext.Set<CardDetailTemplateMatch>().Add(matching);
                        }
                        else
                        {
                            logger.WriteLine("Matching already processed.");
                        }
                    }
                    if (counter > 0 && counter % 100 == 0)
                    {
                        counter++;
                        await dataContext.SaveChangesAsync();
                        logger.WriteLine($"Already matched: {counter}");
                    }
                }
            }

            await dataContext.SaveChangesAsync();

        }


        #region Вспомогательные методы

        private static (bool matched, string? value) ProcessCardWithTemplate(Card card, Template template, List<Size> allSizes, List<Brand> allBrands)
        {
            bool isSuccess;

            Match match = null;
            var isRegex = template.ApplyField == TemplateApplyField.Price;

            if (isRegex)
            {
                Regex templateRegex = new(template.SearchValue);
                match = templateRegex.Match(template.GetCardText(card));

                isSuccess = match.Success;
            }
            else
            {
                isSuccess = template
                    .GetCardText(card)
                    .Contains(template.SearchValue, StringComparison.InvariantCultureIgnoreCase);
            }

            if (isSuccess)
            {
                switch (template.ApplyField)
                {
                    case TemplateApplyField.Price:
                        if (decimal.TryParse(match?.Groups[1].Value, out var price)) // 1 group must be here TODO: debug
                        {
                            return (true, price.ToString());
                        }
                        else
                        {
                            Debug.WriteLine(
                                $"Карточка {card.Id}. " +
                                $"Шаблон {template.Id}. " +
                                $"Значение '{match.Groups[0].Value}' не подходит для цены.");

                            return (false, null);
                        }
                    case TemplateApplyField.Brand:
                        var newBrand = allBrands.FirstOrDefault(x => x.Name == template.ApplyValue);

                        if (newBrand == null)
                        {
                            Debug.WriteLine(
                                $"Карточка {card.Id}. " +
                                $"Шаблон {template.Id}. " +
                                $"Бренд с именем '{template.ApplyValue}' не найден в базе");
                            return (false, null);
                        }

                        return (true, newBrand.Id.ToString());
                    case TemplateApplyField.Color:
                        return (true, template.ApplyValue);
                        break;
                    case TemplateApplyField.Size:
                        List<decimal> templateSetSizeValues;
                        try
                        {
                            templateSetSizeValues = template.ApplyValue
                                .Split(',')
                                .Select(decimal.Parse)
                                .ToList(); // semicolon-seperated values must be here
                        }
                        catch (FormatException)
                        {
                            Debug.WriteLine(
                                $"Карточка {card.Id}. " +
                                $"Шаблон {template.Id}. " +
                                $"Размеры шаблона не могут быть распознаны: '{template.ApplyValue}'");
                            return (false, null);
                        }

                        var newSizes = allSizes
                            .Where(x => templateSetSizeValues.Contains(x.Value))
                            .Select(x => new CardDetailSize
                            {
                                SizeId = x.Id,
                                CardId = card.Id
                            })
                            .ToList();

                        if (newSizes.Count == 0)
                        {
                            return (false, null);
                        }

                        return (true, string.Join(',', newSizes.Select(x => x.SizeId)));
                    default:
                        return (false, null);
                }
            }

            return (false, null);
        }

        private static void SetTemplateMatchingValue(Card card, Template template, string? matchingValue)
        {
            if (!string.IsNullOrEmpty(matchingValue))
            {
                switch (template.ApplyField)
                {
                    case TemplateApplyField.Price:

                        var price = decimal.Parse(matchingValue);
                        Debug.WriteLineIf(card.CardDetail.Price != null && card.CardDetail.Price != price,
                            $"Карточка {card.Id}. " +
                            $"Шаблон {template.Id}. " +
                            $"Значение цены {card.CardDetail.Price} перезаписано новым значением {price}.");

                        card.CardDetail.Price = price;
                        break;
                    case TemplateApplyField.Brand:
                        var newBrandId = int.Parse(matchingValue);
                        Debug.WriteLineIf(card.CardDetail.BrandId != null
                            && newBrandId != card.CardDetail.BrandId,
                                $"Карточка {card.Id}. " +
                                $"Шаблон {template.Id}. " +
                                $"Бренд '{card.CardDetail.BrandId}:{card.CardDetail.Brand.Name}' перезаписан новым значением '{newBrandId}:{template.ApplyValue}'");

                        card.CardDetail.BrandId = newBrandId;
                        break;
                    case TemplateApplyField.Color:
                        Debug.WriteLineIf(card.CardDetail.Color != matchingValue,
                                $"Карточка {card.Id}. " +
                                $"Шаблон {template.Id}. " +
                                $"Цвет '{card.CardDetail.Color}' перезаписан новым значением '{matchingValue}'");

                        card.CardDetail.Color = matchingValue;
                        break;
                    case TemplateApplyField.Size:
                        var newSizeIds = matchingValue
                            .Split(',')
                            .Select(int.Parse)
                            .ToList();
                        var newSizes = newSizeIds.Select(x => new CardDetailSize
                        {
                            SizeId = x,
                            CardId = card.Id
                        });

                        var beforeCount = card.CardDetail.CardDetailSizes.Count;
                        var newCount = newSizeIds.Count;

                        card.CardDetail.CardDetailSizes = card.CardDetail.CardDetailSizes
                            .UnionBy(newSizes, x => x.SizeId)
                            .ToList();

                        var afterCount = card.CardDetail.CardDetailSizes.Count;

                        Debug.WriteLineIf(beforeCount + newCount != afterCount,
                                $"Карточка {card.Id}. " +
                                $"Шаблон {template.Id}. " +
                                $"Размеры пересекаются! Количество размеров до изменения: {beforeCount}, новых: {newCount}, после изменения: {afterCount}");

                        break;
                    default:
                        break;
                }
            }
        }

        #endregion
    }
}