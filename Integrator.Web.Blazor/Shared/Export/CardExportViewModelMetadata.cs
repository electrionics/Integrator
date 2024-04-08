using FluentImpex.Converters.Common.Base;
using Integrator.Shared.FluentImpex;

namespace Integrator.Web.Blazor.Shared.Export
{
    public class CardExportViewModelMetadata : BaseMetadata<CardExportViewModel>
    {
        public CardExportViewModelMetadata() 
        {
            CellValue(x => x.CardExportId, "IE_XML_ID");
            CellValue(x => x.Model, "IE_NAME");
            CellValue(x => x.Active, "IE_ACTIVE");
            CellValue(x => x.PreviewText, "IE_PREVIEW_TEXT");

            CellValue(x => x.Brand, "IC_GROUP0");
            CellValue(x => x.MainCategory, "IC_GROUP1");
            CellValue(x => x.SubCategory, "IC_GROUP2");

            CellValue(x => x.Price, "IP_PROP001");
            CellValue(x => x.Color, "IP_PROP002");
            CellValue(x => x.Material, "IP_PROP003");
            CellValue(x => x.Sizes, "IP_PROP004");
            
            #region Images

            CellValue(x => x.Image1, "IMG1");
            CellValue(x => x.Image2, "IMG2");
            CellValue(x => x.Image3, "IMG3");
            CellValue(x => x.Image4, "IMG4");
            CellValue(x => x.Image5, "IMG5");
            CellValue(x => x.Image6, "IMG6");
            CellValue(x => x.Image7, "IMG7");
            CellValue(x => x.Image8, "IMG8");
            CellValue(x => x.Image9, "IMG9");

            #endregion

            AddConverters(new List<IConverter> { new CustomBoolConverter() });
        }

    }
}
