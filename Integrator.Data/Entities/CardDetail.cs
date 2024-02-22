using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrator.Data.Entities
{
    public class CardDetail
    {
        public int CardId { get; set; }

        public int? CategoryId { get; set; }

        public int? BrandId { get; set; }

        public decimal? Price { get; set; }

        public string? Color { get; set; }

        public List<CardDetailSize> CardDetailSizes { get; set; }


        public Card Card { get; set; }

        public Brand Brand { get; set; }

        public Category Category { get; set; }


        public List<CardDetailTemplateMatch> TemplateMatches { get; set; }
    }
}
