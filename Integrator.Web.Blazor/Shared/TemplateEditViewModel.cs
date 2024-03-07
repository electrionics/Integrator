namespace Integrator.Web.Blazor.Shared
{
    public class TemplateEditViewModel
    {
        public TemplateEditViewModel() 
        {
            ApplyField = 1; //(int)TemplateApplyField.Brand;
            SearchField = 1; //(int)TemplateSearchField.SourceText;
            SearchValue = string.Empty;
        }


        public int Id { get; set; }

        public bool IsRegexp { get; set; }

        public int? SearchField { get; set; }

        public string? SearchValue { get; set; }

        public int? ApplyField { get; set; }

        public string? ApplyValue { get; set; }

        public int? ApplyOrder { get; set; }

        public string? Description { get; set; }
    }
}
