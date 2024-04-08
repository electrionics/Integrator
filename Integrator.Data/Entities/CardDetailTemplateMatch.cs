namespace Integrator.Data.Entities
{
    public class CardDetailTemplateMatch
    {
        public int CardDetailId { get; set; }

        public int TemplateId { get; set; }

        public string? Value { get; set; }

        public int? Order { get;set; }

        public CardDetail CardDetail { get; set; }

        public Template Template { get; set; }
    }
}
