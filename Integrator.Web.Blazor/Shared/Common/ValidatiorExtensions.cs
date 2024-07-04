using FluentValidation;

namespace Integrator.Web.Blazor.Shared.Common
{
    public static class ValidatiorExtensions
    {
        public static IRuleBuilderOptions<T, int?> IsValueInEnum<T, TEnum>(this IRuleBuilderOptions<T, int?> rule)
            where TEnum: Enum
        {
            return rule.Must(x => !x.HasValue || Enum.IsDefined(typeof(TEnum), x.Value));
        }
    }
}
