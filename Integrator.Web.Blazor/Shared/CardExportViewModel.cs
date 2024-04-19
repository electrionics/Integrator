namespace Integrator.Web.Blazor.Shared
{
    public class CardExportViewModel
    {
        public int CardExportId { get; set; }

        public string? Model { get; set; }
        public string BitrixId => string.Empty;

        public bool Active { get; set; } = true;

        public DateTime ActiveFrom { get; set; }

        public string? ActiveTo => string.Empty;

        public string? PreviewPicture => Image1;

        public string? PreviewText => "Самые популярные мировые бренды из Китая";

        public string PreviewTextType => "text";

        public string? DetailPicture => Image1;

        public string? DetailText => "Люксовое качество, товар проверен до отгрузки. Фото и видеоотчеты для клиента.";

        public string DetailTextType => "text";

        public string Url { get; set; }

        public int Sort => 500;

        public string Tags => string.Empty;

        public string? Brand { get; set; }

        public string? MainCategory => string.Empty;

        public string? SubCategory {  get; set; }

        public string? SubCategorySecond => SubCategory;

        public decimal? Price { get; set; }

        public decimal? SecondPrice => Price;

        public string Badge => "New";

        public int? BrandCode { get; set; }

        public string Code { get; set; }

        public string VideoUrl => string.Empty;

        public string AdditionalImages
        {
            get
            {
                var images = new[]
                {
                    Image2,
                    Image3,
                    Image4,
                    Image5,
                    Image6,
                    Image7,
                    Image8,
                    Image9,
                }.Where(x => !string.IsNullOrEmpty(x));

                return string.Join(",", images);
            }
        }

        public string? Material => string.Empty;

        public string Country => "Китай";

        public string? Color { get; set; }

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
