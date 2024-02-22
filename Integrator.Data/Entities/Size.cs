using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrator.Data.Entities
{
    public class Size
    {
        public int Id { get; set; }

        public decimal Value { get; set; }


        public List<CardDetailSize> CardDetailSizes { get; set; }
    }
}
