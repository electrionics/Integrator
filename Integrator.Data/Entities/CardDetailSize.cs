namespace Integrator.Data.Entities
{
    public class CardDetailSize
    {
        public int CardId { get; set; }

        public int SizeId { get; set; }


        public CardDetail CardDetail { get; set; }

        public Size Size { get; set; }
    }
}
