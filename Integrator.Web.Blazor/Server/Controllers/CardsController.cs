using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Integrator.Data;
using Integrator.Data.Entities;
using Integrator.Web.Blazor.Shared;
using Microsoft.AspNetCore.Http.Timeouts;


namespace Integrator.Web.Blazor.Server.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CardsController : ControllerBase
    {
        private readonly ILogger<CardsController> _logger;
        private readonly IntegratorDataContext dataContext;

        public CardsController(ILogger<CardsController> logger, IntegratorDataContext dataContext)
        {
            _logger = logger;
            this.dataContext = dataContext;
        }

        [HttpGet]
        [RequestTimeout(ServerConstants.LongRunningPolicyName)]
        public async Task<CardViewModel[]> GetList()
        {
            try
            {
                var data = await dataContext.Set<Card>().AsNoTracking()
                        .Include(x => x.Translation)
                        .Include(x => x.Shop)
                        .Include(x => x.Images)
                        .Include(x => x.Detail).ThenInclude(x => x.Sizes).ThenInclude(x => x.Size)
                    .Where(x => x.Detail != null)
                    .ToListAsync();

                var result = data
                    .Select(x =>
                    {
                        var cardPath = Path.Join("shops", x.Shop.Name, x.FolderPath);

                        var res = new CardViewModel
                        {
                            CardId = x.Id,

                            CardPathSource = x.FolderPath,
                            CardPathEng = x.Translation.TitleEng,
                            CardPathRus = x.Translation.TitleEng,

                            SourceContent = x.TextFileContent,
                            EngContent = x.Translation.ContentEng,
                            RusContent = x.Translation.ContentRus,

                            ShopName = x.Shop.Name,
                            ShopId = x.Shop.Id,

                            ImageUrls = x.Images.Select(y => Path.Join(cardPath, y.ImageFileName).Replace('\\','/')).ToList(),

                            CategoryId = x.Detail.CategoryId,
                            BrandId = x.Detail.BrandId,

                            SizeValues = x.Detail.Sizes.Select(x => x.Size.Value).ToList(),

                            Color = x.Detail.Color,
                            Price = x.Detail.Price,

                            Information = x.InfoContent
                        };

                        return res;
                    }
                ).ToList();
                    

                var categories = await dataContext.Set<Category>().AsNoTracking().ToDictionaryAsync(x => x.Id);
                var brands = await dataContext.Set<Brand>().AsNoTracking().ToDictionaryAsync(x => x.Id);

                foreach (var item in result)
                {
                    item.SizeStr = string.Join(", ", item.SizeValues);

                    if (item.CategoryId != null)
                    {
                        item.CategoryName = categories[item.CategoryId.Value].Name;
                    }

                    if (item.BrandId != null)
                    {
                        item.BrandName = brands[item.BrandId.Value].Name;
                    }
                }

                return result.ToArray();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

    }
}