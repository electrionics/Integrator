namespace Integrator.Data.Entities
{
    public class Size
    {
        public int Id { get; set; }

        public string Value { get; set; }


        public List<CardDetailSize> CardDetailSizes { get; set; }
    }
}
