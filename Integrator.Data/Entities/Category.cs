namespace Integrator.Data.Entities
{
    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string DisplayName { get; set; }


        public List<CategoryDraft> CategoryDrafts { get; set; }

        public List<CardDetail> CardDetails { get; set; }
    }
}
