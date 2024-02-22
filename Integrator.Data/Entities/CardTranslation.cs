namespace Integrator.Data.Entities
{
    public class CardTranslation
    {
        public int CardId { get; set; }

        public string TitleEng { get; set; }

        public string TitleRus { get; set; }

        public string ContentEng { get; set; }

        public string ContentRus { get; set; }


        public Card Card { get; set; }
    }
}
