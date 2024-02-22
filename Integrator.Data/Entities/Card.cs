namespace Integrator.Data.Entities
{
    public class Card
    {
        public int Id { get; set; }

        public int ShopId { get; set; }

        public string FolderPath { get; set; }

        public string FolderName { get; set; }

        public string TextFileName { get; set; }

        public string TextFileContent { get; set; }

        public string InfoContent { get; set; }


        public Shop Shop { get; set; }

        public CardTranslation CardTranslation { get; set; }

        public List<CardImage> Images { get; set; }

        public CardDetail CardDetail { get; set; }


        public BrandDraft BrandDraft { get; set; }

        public CategoryDraft CategoryDraft { get; set; }
    }
}
