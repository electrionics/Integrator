using FluentValidation;

using Integrator.Data;
using Integrator.Data.Entities;
using Integrator.Web.Blazor.Shared;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Integrator.Web.Blazor.Server.Controllers
{
    [ApiController]
    [Route("[controller]/[action]/{id?}")]
    public class ReplacementsController : ControllerBase
    {
        private readonly ILogger<CardsController> _logger;
        private readonly IntegratorDataContext dataContext;
        private readonly IValidator<ReplacementEditViewModel> _replacementModelValidator;

        public ReplacementsController(ILogger<CardsController> logger, IntegratorDataContext dataContext, IValidator<ReplacementEditViewModel> replacementModelValidator)
        {
            _logger = logger;
            this.dataContext = dataContext;
            _replacementModelValidator = replacementModelValidator;
        }

        [HttpGet]
        public async Task<ReplacementEditViewModel?> Get(int id)
        {
            var replacement = await dataContext.Set<Replacement>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (replacement == null)
            {
                return null;
            }

            var model = new ReplacementEditViewModel
            {
                Id = id,
                SearchValue = replacement.SearchValue,
                ApplyValue = replacement.ApplyValue,
                ApplyOrder = replacement.Order,
                Description = replacement.Description,
            };

            return model;
        }

        [HttpPost]
        public async Task<ReplacementCheckViewModel> Save([FromBody] ReplacementEditViewModel model)
        {
            var validationResult = await _replacementModelValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                return new ReplacementCheckViewModel
                {
                    Errors = validationResult.Errors.ToDictionary(x => x.PropertyName, x => x.ErrorMessage)
                };
            }

            Replacement data;
            if (model.Id != 0)
            {
                data = await dataContext.Set<Replacement>().FirstAsync(x => x.Id == model.Id);
            }
            else
            {
                data = new Replacement();
                dataContext.Set<Replacement>().Add(data);
            }

            data.SearchValue = model.SearchValue;
            data.ApplyValue = model.ApplyValue;

            data.Order = model.ApplyOrder;
            data.Description = model.Description;

            await dataContext.SaveChangesAsync();

            // TODO: calculate how much will affect by search and applying with deletion of old and adding of new template

            return await CheckInternal(data);
        }

        [HttpPost]
        public async Task<ReplacementCheckViewModel> Check([FromBody] ReplacementEditViewModel model)
        {
            var replacement = new Replacement();

            replacement.SearchValue = model.SearchValue;
            replacement.ApplyValue = model.ApplyValue;
            replacement.Order = model.ApplyOrder;

            return await CheckInternal(replacement);
        }

        [HttpGet]
        public async Task<ReplacementCheckViewModel?> CheckExisting(int id)
        {
            var replacement = await dataContext.Set<Replacement>().FirstOrDefaultAsync(x => x.Id == id);

            if (replacement == null)
            {
                return null;
            }

            return await CheckInternal(replacement);
        }

        private async Task<ReplacementCheckViewModel> CheckInternal(Replacement template)
        {
            if (template is null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            var result = new ReplacementCheckViewModel
            {
                Errors = new(),
                CountAffected = 999
            };

            return await Task.FromResult(result);
        }

        [HttpGet]
        public async Task<ReplacementItemViewModel[]> GetList()
        {
            var result = await dataContext.Set<Replacement>().AsNoTracking()
                .Select(x => new ReplacementItemViewModel
                {
                    Id = x.Id,
                    SearchValue = x.SearchValue,
                    ApplyValue = x.ApplyValue,
                    ApplyOrder = x.Order,
                    Description = x.Description,
                    CheckViewModel = new ReplacementCheckViewModel
                    {
                        CountAffected = 999
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
                var data = await dataContext.Set<Replacement>().FirstOrDefaultAsync(x => x.Id == id);

                if (data == null)
                {
                    _logger.LogWarning($"Replacement with id {id} not found. Can't delete.");
                    return false;
                }

                dataContext.Set<Replacement>().Remove(data);
                await dataContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while deleting replacement with id = {id}");
                return false;
            }

            return true;
        }
    }
}
