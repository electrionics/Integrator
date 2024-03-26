namespace Integrator.Data.Entities
{
    public class Replacement
    {
        public int Id { get; set; }

        public string SearchValue { get; set; }

        public string ApplyValue { get; set; }

        public int? Order { get; set; }

        public string? Description { get; set; }


        public void ApplyTo(Card card)
        {
            if (card.Detail != null)
            {
                card.Detail.ContentRus = card.Detail.ContentRus.Replace(SearchValue, ApplyValue);
            }
        }
    }
}
