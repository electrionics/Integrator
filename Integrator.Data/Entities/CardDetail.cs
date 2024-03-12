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

        public List<CardDetailSize> CardDetailSizes { get; set; }


        public Card Card { get; set; }

        public Brand Brand { get; set; }

        public Category Category { get; set; }


        public List<CardDetailTemplateMatch> TemplateMatches { get; set; }
    }
}
