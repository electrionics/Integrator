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
            if (card.CardDetail != null)
            {
                card.CardDetail.ContentRus = card.CardDetail.ContentRus.Replace(SearchValue, ApplyValue);
            }
        }
    }
}
