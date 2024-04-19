using FluentImpex.Converters.Common.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrator.Web.Blazor.Shared.Export
{
    public class CustomDateConverter : DateTimeConverter
    {
        public CustomDateConverter() : base("dd.MM.yyyy")
        {
        }
    }
}
