using FluentImpex.Converters.Common.Base;
using Integrator.Shared.FluentImpex;

namespace Integrator.Logic.Export
{
    public class CardExportViewModelMetadata : BaseMetadata<CardExportViewModel>
    {
        public CardExportViewModelMetadata() 
        {
            CellValue(x => x.CardExportId, "IE_XML_ID");
            CellValue(x => x.Model, "IE_NAME");
            CellValue(x => x.ExternalId, "IE_ID");

            CellValue(x => x.Active, "IE_ACTIVE");
            CellValue(x => x.ActiveFrom, "IE_ACTIVE_FROM");
            CellValue(x => x.ActiveTo, "IE_ACTIVE_TO");

            CellValue(x => x.PreviewPicture, "IE_PREVIEW_PICTURE");
            CellValue(x => x.PreviewText, "IE_PREVIEW_TEXT");
            CellValue(x => x.PreviewTextType, "IE_PREVIEW_TEXT_TYPE");

            CellValue(x => x.DetailPicture, "IE_DETAIL_PICTURE");
            CellValue(x => x.DetailText, "IE_DETAIL_TEXT");
            CellValue(x => x.DetailTextType, "IE_DETAIL_TEXT_TYPE");

            CellValue(x => x.Url, "IE_CODE");
            CellValue(x => x.Sort, "IE_SORT");
            CellValue(x => x.Tags, "IE_TAGS");
            CellValue(x => x.Price, "IP_PROP871");
            CellValue(x => x.SecondPrice, "IP_PROP872");
            CellValue(x => x.Badge, "IP_PROP873");
            CellValue(x => x.BrandCode, "IP_PROP874");
            CellValue(x => x.Code, "IP_PROP894");
            CellValue(x => x.VideoUrl, "IP_PROP890");
            CellValue(x => x.AdditionalImages, "IP_PROP898");
            CellValue(x => x.SubCategory, "IP_PROP882");
            CellValue(x => x.Material, "IP_PROP922");
            CellValue(x => x.Country, "IP_PROP925");

            CellValue(x => x.MainCategory, "IC_GROUP0");
            CellValue(x => x.SubCategorySecond, "IC_GROUP1");
            CellValue(x => x.Brand, "IC_GROUP2");

            CellValue(x => x.Color, "IE_COLOR");
            CellValue(x => x.Sizes, "IE_SIZES");

            AddConverters(new List<IConverter> { new CustomBoolConverter(), new CustomDateConverter() });
        }

    }
}
