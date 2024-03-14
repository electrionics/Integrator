using FluentValidation;

namespace Integrator.Web.Blazor.Shared.Validators
{
    public class ReplacementEditViewModelValidator : AbstractValidator<ReplacementEditViewModel>
    {
        public ReplacementEditViewModelValidator()
        {
            RuleFor(x => x.SearchValue)
                .NotEmpty()
                    .WithMessage((m) => "Тескт поиска не должен" +
                    " быть пустым.")
                .MaximumLength(300)
                    .WithMessage((m) => "Тескт поиска не должен" +
                    " превышать в длину 300 символов.");
            RuleFor(x => x.ApplyValue)
                //.NotEmpty()
                //    .WithMessage("Значение применения не должно быть пустым.")
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
