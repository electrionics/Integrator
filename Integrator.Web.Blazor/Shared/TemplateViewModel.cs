using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrator.Web.Blazor.Shared
{
    public class TemplateViewModel
    {
        public int Id { get; set; }

        public bool IsRegexp { get; set; }

        public int SearchField { get; set; }

        public string SearchValue { get; set; }

        public string? ApplyValue { get; set; }

        public int ApplyField { get; set; }

        public int? ApplyOrder { get; set; }

        public string? Description { get; set; }
    }
}
