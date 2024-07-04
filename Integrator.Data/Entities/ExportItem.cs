namespace Integrator.Data.Entities
{
    public class ExportItem
    {
        public string ExternalFileId { get; set; }

        public string FileName { get; set; }

        public bool IsSelected { get; set; }

        public DateTime Created { get; set; }

        public int ExcludedAsRepeatableCount { get; set; }

        public int NoBrandAndCategoryCount { get; set; }

        public int NonPriced { get; set; }
    }
}
