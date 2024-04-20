using FluentImpex.Converters.Common.Default;

namespace Integrator.Logic.Export
{
    public class CustomDateConverter : DateTimeConverter
    {
        public CustomDateConverter() : base("dd.MM.yyyy")
        {
        }
    }
}
