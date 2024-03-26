namespace Integrator.Web.Blazor.Shared
{
    public class CardExportViewModel
    {
        public int CardExportId { get; set; }

        public string? Model { get; set; }

        public bool Active { get; set; } = true;

        public string? PreviewText { get; set; }

        public string? Brand { get; set; }

        public string? MainCategory { get;set; }

        public string? SubCategory {  get; set; }

        public decimal? Price { get; set; }

        public string? Color { get; set; }

        public string? Material { get; set; }

        public string? Sizes { get; set; }

        #region Images

        public string? Image1 { get; set; }

        public string? Image2 { get; set; }
        
        public string? Image3 { get; set; }

        public string? Image4 { get; set; }

        public string? Image5 { get; set; }

        public string? Image6 { get; set; }

        public string? Image7 { get; set; }

        public string? Image8 { get; set; }

        public string? Image9 { get; set; }

        #endregion
    }
}
