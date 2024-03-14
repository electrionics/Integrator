namespace Integrator.Web.Blazor.Shared
{
    public class ReplacementItemViewModel
    {
        public int Id { get; set; }

        public string SearchValue { get; set; }

        public string? ApplyValue { get; set; }

        public int? ApplyOrder { get; set; }

        public string? Description { get; set; }

        public ReplacementCheckViewModel CheckViewModel { get; set; }
    }
}
