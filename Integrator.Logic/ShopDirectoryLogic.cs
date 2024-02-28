using System.Text;
using Microsoft.EntityFrameworkCore;

using Integrator.Data.Entities;
using Integrator.Data.Helpers;
using Integrator.Data;
using Integrator.Shared;

namespace Integrator.Logic
{
    public class ShopDirectoryLogic
    {
        private readonly IntegratorDataContext dataContext;
        private readonly ILogger logger;

        public ShopDirectoryLogic(IntegratorDataContext dataContext, ILogger logger)
        {
            this.dataContext = dataContext;
            this.logger = logger;
        }

        // Загрузка папки для выбранного магазина.
        // Имя магазина в базе должно совпадать с именем папки. Папка лежит в корне приложения.
        // Отдельно загружаются папки с брендами и категориями.
        public async Task LoadShop(string shopName)
        {
            logger.WriteLine($"Load shop: {shopName}");

            var shop = await dataContext.Set<Shop>().FirstOrDefaultAsync(x => x.Name == shopName && !x.Cards.Any());
            if (shop != null)
            {
                var topDirs = Directory.GetDirectories($"shops\\{shopName}"); //TODO: debug

                var brandDir = topDirs.FirstOrDefault(x => x.Contains("brands"));
                var categoryDir = topDirs.FirstOrDefault(x => x.Contains("categories"));

                if (brandDir != null)
                {
                    await ProcessRootDirectory(brandDir, dataContext, shop);
                }
                if (categoryDir != null)
                {
                    await ProcessRootDirectory(categoryDir, dataContext, shop);
                }
            }
        }

        // Загрузка папок с карточками товаров из файловой системы в базу
        private async Task ProcessRootDirectory(string rootDir, IntegratorDataContext dataContext, Shop shop)
        {
            var rootDirInfo = new DirectoryInfo(rootDir);

            var parent = rootDirInfo.Parent;

            var existingCardPaths = await dataContext.Set<Card>()
                .AsNoTracking()
                .Where(x => x.ShopId == shop.Id)
                .Select(x => x.FolderPath)
                .ToListAsync();

            foreach (var directory in rootDirInfo.GetDirectories("*", SearchOption.AllDirectories))
            {
                var file = directory.GetFiles("*.txt", SearchOption.TopDirectoryOnly).SingleOrDefault();
                if (file != null)
                {
                    using var stream = file.OpenRead();
                    using var textReader = new StreamReader(stream, Encoding.GetEncoding("gb2312"));
                    var content = await textReader.ReadToEndAsync();

                    var folderPath = directory.FullName.Replace(parent.FullName, string.Empty);

                    if (!existingCardPaths.Contains(folderPath)) // TODO: test
                    {
                        var card = new Card
                        {
                            FolderName = directory.Name,
                            FolderPath = folderPath,
                            TextFileName = file.Name,
                            TextFileContent = content,
                            InfoContent = StringHelper.RemoveExtraSymbols(content, "   |   "),

                            Shop = shop
                        };

                        var images = directory.GetFiles()
                            .Where(x => x.Name != file.Name)
                            .Select(x => new CardImage
                            {
                                FolderPath = directory.FullName.Replace(parent.FullName, string.Empty),
                                FolderName = directory.Name,
                                ImageFileName = x.Name,
                                ImageFileHash = null
                            })
                            .ToList();
                        card.Images = images;

                        dataContext.Set<Card>().Add(card);
                    }
                }

                logger.WriteLine($"directory {directory.Name}, file {file?.Name ?? "-----"}");
            }

            await dataContext.SaveChangesAsync();
        }
    }
}
