namespace Integrator.Data.Entities
{
    public class CardImage
    {
        public int Id { get; set; }

        public int CardId { get; set; }

        public string FolderName { get; set; }

        public string FolderPath { get; set; }

        public string ImageFileName { get; set; }

        public string? ImageFileHash { get; set; }

        public long? FileSizeBytes { get; set; }


        public Card Card { get; set; }


        public List<CardSimilar> SimilarImages { get; set; }

        public List<CardSimilar> BaseImages { get; set; }
    }
}
