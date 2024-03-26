using FluentImpex.Converters.Common.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrator.Web.Blazor.Shared.Export
{
    internal class CustomBoolConverter : BoolConverter
    {
        public override string ConvertValue(object value)
        {
            var valueBool = (bool?)value;
            return valueBool == true
                ? "Y"
                : valueBool == false
                    ? "N"
                    : string.Empty;
        }
    }
}
