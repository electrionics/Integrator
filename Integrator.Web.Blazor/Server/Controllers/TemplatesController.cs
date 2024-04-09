using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Integrator.Data;
using Integrator.Data.Entities;
using Integrator.Web.Blazor.Shared;
using FluentValidation;
using System.Text.RegularExpressions;

namespace Integrator.Web.Blazor.Server.Controllers
{
    [ApiController]
    [Route("[controller]/[action]/{id?}")]
    public class TemplatesController : ControllerBase
    {
        private readonly ILogger<CardsController> _logger;
        private readonly IntegratorDataContext dataContext;

        private readonly IValidator<TemplateEditViewModel> _templateEditValidator;

        public TemplatesController(ILogger<CardsController> logger, 
            IntegratorDataContext dataContext, 
            IValidator<TemplateEditViewModel> templateEditValidator)
        {
            _logger = logger;
            this.dataContext = dataContext;
            _templateEditValidator = templateEditValidator;
        }

        [HttpGet]
        public async Task<TemplateEditViewModel?> Get(int id)
        {
            var template = await dataContext.Set<Template>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (template == null)
            {
                return null;
            }

            var model = new TemplateEditViewModel
            {
                Id = id,
                SearchField = (int)template.SearchField,
                SearchValue = template.SearchValue,
                ApplyField = (int)template.ApplyField,
                ApplyValue = template.ApplyValue,
                IsRegexp = template.IsRegexp,
                ApplyOrder = template.Order,
                Description = template.Description,
            };

            return model;
        }

        [HttpPost]
        public async Task<TemplateCheckViewModel> Save([FromBody] TemplateEditViewModel model)
        {
            var validationResult = await _templateEditValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                return new TemplateCheckViewModel
                {
                    Errors = validationResult.Errors.ToDictionary(x => x.PropertyName, x => x.ErrorMessage)
                };
            }

            Template data;
            if (model.Id != 0)
            {
                data = await dataContext.Set<Template>().FirstAsync(x => x.Id == model.Id);
            }
            else
            {
                data = new Template();
                dataContext.Set<Template>().Add(data);
            }

            data.IsRegexp = model.IsRegexp;
            data.SearchField = (TemplateSearchField)model.SearchField.Value;
            data.SearchValue = model.SearchValue;

            data.ApplyField = (TemplateApplyField)model.ApplyField.Value;
            data.ApplyValue = model.ApplyValue;

            data.Order = model.ApplyOrder;
            data.Description = model.Description;

            await dataContext.SaveChangesAsync();

            // TODO: calculate how much will affect by search and applying with deletion of old and adding of new template

            return await CheckInternal(data);
        }

        // не учитывает разницу между старым и новым шаблоном, возвращает количество найденных карточек
        // по критериям шаблона с формы, и количество карточек, к которым будет применено значение
        // именно из этого шаблона (при конкуренции учитывается порядок)
        [HttpPost]
        public async Task<TemplateCheckViewModel> Check([FromBody] TemplateEditViewModel model)
        {
            var template = new Template();

            template.IsRegexp = model.IsRegexp;
            template.SearchField = (TemplateSearchField)model.SearchField.Value;
            template.SearchValue = model.SearchValue;

            template.ApplyField = (TemplateApplyField)model.ApplyField.Value;
            template.ApplyValue = model.ApplyValue;

            template.Order = model.ApplyOrder;

            return await CheckInternal(template);
        }

        [HttpGet]
        public async Task<TemplateCheckViewModel?> CheckExisting(int id)
        {
            var template = await dataContext.Set<Template>().FirstOrDefaultAsync(x => x.Id == id);

            if (template == null)
            {
                return null;
            }

            return await CheckInternal(template);
        }

        private async Task<TemplateCheckViewModel> CheckInternal(Template template)
        {
            if (template is null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            var regexp = template.IsRegexp ? new Regex(template.SearchValue) : null;

            var result = new TemplateCheckViewModel
            {
                Errors = new()
            };

            try
            {
                var affectQuery = dataContext.Set<Card>()
                    .AsNoTracking()
                    .Where(x => x.Detail != null && x.Translation != null)
                    .Select(template.GetCardProperty);
                var resultQuery = dataContext.Set<Card>()
                    .AsNoTracking()
                    .Include(x => x.Detail).ThenInclude(x => x.TemplateMatches).ThenInclude(x => x.Template)
                    .Where(x => x.Detail != null && x.Translation != null)
                    .Where(x => !x.Detail.TemplateMatches.Any(y => y.Template.ApplyField == template.ApplyField && y.TemplateId != template.Id) ||
                                x.Detail.TemplateMatches
                                    .Where(y => y.Template.ApplyField == template.ApplyField && y.TemplateId != template.Id)
                                    .OrderBy(y => y.Template.Order)
                                    .Last().Order < template.Order || 
                                (
                                    x.Detail.TemplateMatches
                                        .Where(y => y.Template.ApplyField == template.ApplyField && y.TemplateId != template.Id)
                                        .Max(y => y.Template.Order) == template.Order && 
                                    (
                                        template.Id == 0 ||
                                        x.Detail.TemplateMatches
                                            .Where(y => y.Template.ApplyField == template.ApplyField && y.TemplateId != template.Id)
                                            .OrderBy(y => y.Template.Order)
                                            .Last().TemplateId <= template.Id)
                                    )
                                )// отличие от предыдущего показателя
                    .Select(template.GetCardProperty);

                if (regexp != null)
                {
                    var affectedItems = await affectQuery
                        .ToListAsync();
                    var resultedItems = await resultQuery
                        .ToListAsync();

                    result.CountAffected = affectedItems
                        .Count(x => regexp.IsMatch(x));
                    result.CountResulted = resultedItems
                        .Count(x => regexp.IsMatch(x));
                }
                else
                {
                    result.CountAffected = await affectQuery.CountAsync(x => x.Contains(template.SearchValue));
                    result.CountResulted = await resultQuery.CountAsync(x => x.Contains(template.SearchValue));
                }
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, $"Не удалось посчитать количество карточек для шаблона с номером {template.Id}.");
            }

            return result;
        }

        [HttpGet]
        public IEnumerable<KeyValuePair<int, string>> GetAvailableSearchFields()
        {
            return new List<KeyValuePair<int, string>>()
            {
                new ((int)TemplateSearchField.SourcePath, "Путь"),
                new ((int)TemplateSearchField.RusText, "Текст (рус.)"),
                new ((int)TemplateSearchField.EngText, "Текст (англ.)"),
                new ((int)TemplateSearchField.SourceText, "Текст"),
                new ((int)TemplateSearchField.EngPath, "Путь (англ.)"),
                new ((int)TemplateSearchField.RusPath, "Путь (рус.)"),
            };
        }

        [HttpGet]
        public IEnumerable<KeyValuePair<int, string>> GetAvailableApplyFields()
        {
            return new List<KeyValuePair<int, string>>
            {
                new((int)TemplateApplyField.Brand, "Бренд"),
                new((int)TemplateApplyField.Category, "Категория"),
                new((int)TemplateApplyField.Size, "Размер"),
                new((int)TemplateApplyField.Price, "Цена"),
                new((int)TemplateApplyField.Color, "Цвет"),
                //new((int)TemplateApplyField.Model, "Модель"),
                //new((int)TemplateApplyField.Material, "Материал"),
            };
        }

        [HttpGet]
        public async Task<TemplateItemViewModel[]> GetList()
        {
            var result = await dataContext.Set<Template>().AsNoTracking()
                .Include(x => x.CardDetailMatches)
                .OrderBy(x => x.Order)
                .Select(x => new TemplateItemViewModel
                {
                    Id = x.Id,
                    IsRegexp = x.IsRegexp,
                    SearchField = (int)x.SearchField,
                    SearchValue = x.SearchValue,
                    ApplyField = (int)x.ApplyField,
                    ApplyValue = x.ApplyValue,
                    ApplyOrder = x.Order,
                    Description = x.Description,
                    CheckViewModel = new TemplateCheckViewModel
                    {
                        CountAffected = x.CardDetailMatches.Count,
                        CountResulted = x.CardDetailMatches.Select(x => x.CardDetailId).Distinct().Count()
                    }
                })
                .ToArrayAsync();

            return result;
        }

        [HttpPost]
        public async Task<bool> Delete(int id)
        {
            try
            {
                var data = await dataContext.Set<Template>().Include(x => x.CardDetailMatches).FirstOrDefaultAsync(x => x.Id == id);

                if (data == null)
                {
                    _logger.LogWarning($"Template with id {id} not found. Can't delete.");
                    return false;
                }

                dataContext.Set<Template>().Remove(data);
                await dataContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while deleting template with id = {id}");
                return false;
            }

            return true;
        }
    }
}
