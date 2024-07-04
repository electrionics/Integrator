using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Integrator.Data;
using Integrator.Data.Entities;

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
                .Include(x => x.Translation).Include(x => x.Detail)
                .Where(x => x.Translation != null)
                .ToListAsync();

            var replacements = await dataContext.Set<Replacement>().AsNoTracking().OrderBy(x => x.Order).ToListAsync();

            foreach (var card in cards) 
            {
                if (card.Detail == null)
                {
                    card.Detail = new();
                }

                card.Detail.ContentRus = card.Translation.ContentRus;

                foreach (var replacement in replacements)
                {
                    replacement.ApplyTo(card);
                }
            }

            await dataContext.SaveChangesAsync();
        }
    }
}
