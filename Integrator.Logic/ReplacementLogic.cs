using Integrator.Data;
using Integrator.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrator.Logic
{
    public class ReplacementLogic
    {
        private readonly IntegratorDataContext dataContext;
        private readonly ILogger<ReplacementLogic> logger;

        public ReplacementLogic(IntegratorDataContext dataContext, ILogger<ReplacementLogic> logger)
        {
            this.dataContext = dataContext;
            this.logger = logger;
        }

        public async Task ProcessCardsWithReplacements()
        {
            var cards = await dataContext.Set<Card>()
                .Include(x => x.CardTranslation).Include(x => x.CardDetail)
                .Where(x => x.CardTranslation != null)
                .ToListAsync();

            var replacements = await dataContext.Set<Replacement>().AsNoTracking().OrderBy(x => x.Order).ToListAsync();

            foreach (var card in cards) 
            {
                if (card.CardDetail == null)
                {
                    card.CardDetail = new();
                }

                card.CardDetail.ContentRus = card.CardTranslation.ContentRus;

                foreach (var replacement in replacements)
                {
                    replacement.ApplyTo(card);
                }
            }

            await dataContext.SaveChangesAsync();
        }
    }
}
