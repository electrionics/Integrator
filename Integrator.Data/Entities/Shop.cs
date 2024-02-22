namespace Integrator.Data.Entities
{
    public class Shop
    {
        public int Id { get; set; }

        public string Name { get; set; }


        public List<Card> Cards { get; set; }
    }
}
