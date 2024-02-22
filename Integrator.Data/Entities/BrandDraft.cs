namespace Integrator.Data.Entities
{
    public class BrandDraft : ToponomyDraft
    {
        public int? BrandId { get; set; }


        protected override bool IsFirstPathNameToSelect => true;

        protected override string ToponomyParentString => "brands\\";


        public BrandDraft() : base() { }

        public BrandDraft(Card card) : base(card) { }


        public Brand? Brand { get; set; }

        public Card Card { get; set; }
    }
}
