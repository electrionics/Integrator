namespace Integrator.Web.Blazor.Shared
{
    public class ExportItemViewModel
    {
        public string FileName { get; set; }

        public string ExternalFileId { get; set; }

        public DateTime DateTimeGenerated { get; set; }

        public bool IsSelected { get; set; }

        public string IsSelectedStatus => IsSelected ? "Выбранный" : "";

        public ExportItemReport FileReport { get; set; }
    }

    public class ExportItemReport
    {
        public int ExcludedAsRepeatableCount { get; set; }

        public int NoBrandAndCategoryCount { get; set; }

        public int NonPriced { get; set; }
    }
}
