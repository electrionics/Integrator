namespace Integrator.Data.Entities
{
    public class CardSimilar
    {
        public int BaseCardImageId { get; set; }

        public int SimilarCardImageId { get; set; }


        public int BaseCardId { get; set; }

        public int SimilarCardId { get; set; }


        public CardImage BaseImage { get; set; }

        public CardImage SimilarImage { get; set; }


        public Card BaseCard { get; set; }

        public Card SimilarCard { get; set; }
    }
}
