using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
