using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrator.Web.Blazor.Shared
{
    public class ReplacementCheckViewModel
    {
        /// <summary>
        /// количество карточек, совпавших с заменителем по тексту поиска
        /// </summary>
        public int CountAffected { get; set; }

        /// <summary>
        /// Сумма произведенных замен по каждой совпавшей карточке
        /// </summary>
        public int CountMatched { get; set; }

        public Dictionary<string, string> Errors { get; set; }
    }
}
