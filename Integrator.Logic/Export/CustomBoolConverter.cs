using FluentImpex.Converters.Common.Default;

namespace Integrator.Logic.Export
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
