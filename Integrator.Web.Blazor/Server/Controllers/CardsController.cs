using Integrator.Data;
using Integrator.Data.Entities;
using Integrator.Web.Blazor.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<CardViewModel[]> GetList()
        {
            try
            {
                var data = await dataContext.Set<Card>().AsNoTracking()
                        .Include(x => x.CardTranslation)
                        .Include(x => x.Shop)
                        .Include(x => x.Images)
                        .Include(x => x.CardDetail).ThenInclude(x => x.CardDetailSizes).ThenInclude(x => x.Size)
                    .Where(x => x.CardDetail != null)
                    .ToListAsync();

                var result = data
                    .Select(x =>
                    {
                        var cardPath = Path.Join("shops", x.Shop.Name, x.FolderPath);

                        var res = new CardViewModel
                        {
                            CardId = x.Id,

                            CardPathSource = x.FolderPath,
                            CardPathEng = x.CardTranslation.TitleEng,
                            CardPathRus = x.CardTranslation.TitleEng,

                            SourceContent = x.TextFileContent,
                            EngContent = x.CardTranslation.ContentEng,
                            RusContent = x.CardTranslation.ContentRus,

                            ShopName = x.Shop.Name,
                            ShopId = x.Shop.Id,

                            ImageUrls = x.Images.Select(y => Path.Join(cardPath, y.ImageFileName).Replace('\\','/')).ToList(),

                            CategoryId = x.CardDetail.CategoryId,
                            BrandId = x.CardDetail.BrandId,

                            SizeValues = x.CardDetail.CardDetailSizes.Select(x => x.Size.Value).ToList(),

                            Color = x.CardDetail.Color,
                            Price = x.CardDetail.Price,

                            Information = x.InfoContent
                        };

                        return res;
                    }
                ).ToList();
                    

                var categories = await dataContext.Set<Category>().AsNoTracking().ToDictionaryAsync(x => x.Id);
                var brands = await dataContext.Set<Brand>().AsNoTracking().ToDictionaryAsync(x => x.Id);

                foreach (var item in result)
                {
                    item.SizeStr = string.Join(", ", item.SizeValues.Select(x => x.ToString("#.#")));

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

        [HttpGet]
        public IEnumerable<KeyValuePair<int, string>> GetAvailableSearchFields()
        {
            return new List<KeyValuePair<int, string>>()
            {
                new ((int)TemplateSearchField.SourcePath, "Исходный путь"),
                new ((int)TemplateSearchField.RusText, "Русский текст"),
                new ((int)TemplateSearchField.EngText, "Английский текст"),
                new ((int)TemplateSearchField.SourceText, "Исходный текст"),
                new ((int)TemplateSearchField.EngPath, "Английский путь"),
                new ((int)TemplateSearchField.RusPath, "Русский путь"),
            };
        }

        [HttpGet]
        public IEnumerable<KeyValuePair<int, string>> GetAvailableApplyFields()
        {
            return new List<KeyValuePair<int, string>>
            {
                new((int)TemplateApplyField.Brand, "Бренд"),
                new((int)TemplateApplyField.Size, "Размер"),
                new((int)TemplateApplyField.Color, "Цвет"),
                new((int)TemplateApplyField.Price, "Цена"),
            };
        }

        [HttpPost]
        public async Task<TemplateCheckViewModel> SaveTemplate([FromBody]TemplateViewModel model)
        {
            Template data;
            if (model.Id != 0)
            {
                data = await dataContext.Set<Template>().FirstAsync(x => x.Id == model.Id);
            }
            else
            {
                data = new Template();
            }

            data.SearchField = (TemplateSearchField)model.SearchField;
            data.SearchValue = model.SearchValue;

            data.ApplyField = (TemplateApplyField)model.ApplyField;
            data.ApplyValue = model.ApplyValue;

            data.Order = model.ApplyOrder;
            data.Description = model.Description;

            throw new NotImplementedException();
        }


    }
}