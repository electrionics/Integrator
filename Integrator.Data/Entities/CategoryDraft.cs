namespace Integrator.Data.Entities
{
    public class CategoryDraft : ToponomyDraft
    {
        public int? CategoryId { get; set; }


        protected override bool IsFirstPathNameToSelect => false;

        protected override string ToponomyParentString => "categories\\";


        public CategoryDraft() : base() { }

        public CategoryDraft(Card card) : base(card) { }


        public Card Card { get; set; }

        public Category Category { get; set; }
    }
}
