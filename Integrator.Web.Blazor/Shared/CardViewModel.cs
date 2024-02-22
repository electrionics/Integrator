namespace Integrator.Web.Blazor.Shared
{
    public class CardViewModel
    {
        #region Basic Information

        public int CardId { get; set; }

        public int ShopId { get; set; }

        public string ShopName { get; set; }

        public string CardPathSource { get; set; }

        public string CardPathEng { get; set; }

        public string CardPathRus { get; set; }

        public List<string> ImageUrls { get; set; }

        #endregion


        #region Recognized Information

        public int? CategoryId { get; set; }

        public string? CategoryName { get; set; }

        public int? BrandId { get; set; }

        public string? BrandName { get; set; }

        public List<decimal> SizeValues { get; set; }

        public string? SizeStr { get; set; }

        public decimal? Price { get; set; }

        public string? Color { get; set; }

        #endregion


        #region Text Content

        public string SourceContent { get; set; }

        public string EngContent { get; set; }

        public string RusContent { get; set; }

        #endregion

        public string Information { get; set; }
    }
}
