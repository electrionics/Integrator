using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
