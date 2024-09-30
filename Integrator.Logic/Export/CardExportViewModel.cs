using CsvHelper.Configuration.Attributes;
using System.Globalization;

namespace Integrator.Logic.Export
{
    [Delimiter(";")]
    [CultureInfo("ru-RU")]
    public class CardExportViewModel
    {
        [Name("IE_XML_ID")]
        public int CardExportId { get; set; }

        [Name("IE_NAME")]
        public string? Model { get; set; }

        [Name("IE_ID")]
        public string ExternalId => string.Empty;

        [Name("IE_ACTIVE")]
        [BooleanTrueValues("Y")]
        [BooleanFalseValues("N")]
        public bool Active => true;

        [Name("IE_ACTIVE_FROM")]
        public string ActiveFrom => ActiveFromDate.ToString("dd.MM.yyyy");

        [Ignore]
        public DateTime ActiveFromDate { get; set; }

        [Name("IE_ACTIVE_TO")]
        public string? ActiveTo => string.Empty;

        [Name("IE_PREVIEW_PICTURE")]
        public string? PreviewPicture => Image1;

        [Name("IE_PREVIEW_TEXT")]
        public string? PreviewText { get; set; } = "Самые популярные мировые бренды из Китая";

        [Name("IE_PREVIEW_TEXT_TYPE")]
        public string PreviewTextType { get; set; } = "html";

        [Name("IE_DETAIL_PICTURE")]
        public string? DetailPicture => Image1;

        [Name("IE_DETAIL_TEXT")]
        public string? DetailText { get; set; } = "Люксовое качество, товар проверен до отгрузки. Фото и видеоотчеты для клиента.";

        [Name("IE_DETAIL_TEXT_TYPE")]
        public string DetailTextType { get; set; } = "html";

        [Name("IE_CODE")]
        public string Url { get; set; }

        [Name("IE_SORT")]
        public int Sort => 500;

        [Name("IE_TAGS")]
        public string Tags => string.Empty;

        [Name("IP_PROP871")]
        public decimal? Price { get; set; }

        [Name("IP_PROP872")]
        public decimal? SecondPrice => Price;

        [Name("IP_PROP873")]
        public string Badge { get; set; } = "New";

        [Name("IP_PROP874")]
        public int? BrandCode { get; set; }

        [Name("IP_PROP894")]
        public string Code { get; set; }

        [Name("IP_PROP890")]
        public string VideoUrl => string.Empty;

        [Name("IP_PROP898")]
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

        [Name("IP_PROP882")]
        public string? SubCategory { get; set; }

        [Name("IP_PROP922")]
        public string? Material => string.Empty;

        [Name("IP_PROP925")]
        public string Country { get; set; } = "Китай";

        [Name("IC_GROUP0")]
        public string? MainCategory => string.Empty;

        [Name("IC_GROUP1")]
        public string? SubCategorySecond => SubCategory;

        [Name("IC_GROUP2")]
        public string? Brand { get; set; }

        [Name("IE_COLOR")]
        public string? Color { get; set; }

        [Name("IE_SIZES")]
        public string? Sizes { get; set; }

        #region Images

        [Ignore]
        public string? Image1 { get; set; }

        [Ignore]
        public string? Image2 { get; set; }

        [Ignore]
        public string? Image3 { get; set; }

        [Ignore]
        public string? Image4 { get; set; }

        [Ignore]
        public string? Image5 { get; set; }

        [Ignore]
        public string? Image6 { get; set; }

        [Ignore]
        public string? Image7 { get; set; }

        [Ignore]
        public string? Image8 { get; set; }

        [Ignore]
        public string? Image9 { get; set; }

        #endregion
    }
}
