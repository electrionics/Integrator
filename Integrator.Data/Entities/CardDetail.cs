namespace Integrator.Data.Entities
{
    public class CardDetail
    {
        public int CardId { get; set; }

        public int? CategoryId { get; set; }

        public int? BrandId { get; set; }

        public decimal? Price { get; set; }

        public string? Color { get; set; }

        public string? Model { get; set; }

        public string? Material { get; set; }

        public string ContentRus { get; set; }

        public List<CardDetailSize> Sizes { get; set; }


        #region Rating

        private const decimal ModelSetAddition = 1.0m;
        private const decimal SizesSetAddition = 0.8m;
        private const decimal ColorSetAddition = 0.8m;
        private const decimal PriceSetAddition = 0.5m;
        private const decimal MaterialSetAddition = 0.3m;

        public decimal Rating => (Model is null ? 0 : ModelSetAddition) + 
            ((Sizes?.Count ?? 0) == 0 ? 0 : SizesSetAddition) + 
            (Color is null ? 0 : ColorSetAddition) +
            (Price is null ? 0 : PriceSetAddition) +
            (Material is null ? 0 : MaterialSetAddition);

        #endregion

        public Card Card { get; set; }

        public Brand? Brand { get; set; }

        public Category? Category { get; set; }


        public List<CardDetailTemplateMatch> TemplateMatches { get; set; }
    }
}
