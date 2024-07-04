using FluentValidation;

using Integrator.Data.Entities;
using Integrator.Web.Blazor.Shared.Common;

namespace Integrator.Web.Blazor.Shared.Validators
{
    public class TemplateEditViewModelValidator : AbstractValidator<TemplateEditViewModel>
    {
        public TemplateEditViewModelValidator() 
        { 
            RuleFor(x => x.SearchField)
                .NotEmpty()
                    .WithMessage("Выберите поле для поиска.")
                .IsValueInEnum<TemplateEditViewModel, TemplateSearchField>()
                    .WithMessage("Некорректно выбранное значение.");
            RuleFor(x => x.SearchValue)
                .NotEmpty()
                    .WithMessage((m) => (m.IsRegexp ? "Регулярное выражение поиска не должно" : "Тескт поиска не должен") + 
                    " быть пустым.")
                .MaximumLength(300)
                    .WithMessage((m) => (m.IsRegexp ? "Регулярное выражение поиска не должно" : "Тескт поиска не должен") +
                    " превышать в длину 300 символов.");
            RuleFor(x => x.ApplyField)
                .NotEmpty()
                    .WithMessage("Выберите поле для применения.")
                .IsValueInEnum<TemplateEditViewModel, TemplateApplyField>()
                    .WithMessage("Некорректно выбранное значение.");
            RuleFor(x => x.ApplyValue)
                .NotEmpty()
                    .WithMessage("Значение применения не должно быть пустым.")
                    .Unless(x => x.IsRegexp)
                .MaximumLength(300)
                    .WithMessage("Значение применения не должно превышать в длину 300 символов.");
                
            RuleFor(x => x.ApplyOrder)
                .InclusiveBetween(0, 1000)
                    .WithMessage("Порядок применения не должен выходить за диапазон от 0 до 1000.");
            RuleFor(x => x.Description)
                .MaximumLength(1000)
                    .WithMessage("Описание не должно быть больше 1000 символов в длину.");
        }
    }
}
