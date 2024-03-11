﻿using System.Text;
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

        /// <summary>
        /// Синхронизация всех папок магазинов в папке shops.
        /// </summary>
        /// <returns>Успешно (частично или полностью) обработан каждый магазин, либо один или более обработаны с ошибками уровня магазина/топономий, или не обработаны</returns>
        public async Task<bool> SyncShopsRoot()
        {
            var shopFolderDirectories = Directory.GetDirectories($"shops");
            var completeSuccess = true;

            foreach (var shopDirectory  in shopFolderDirectories )
            {
                completeSuccess &= await SyncShopDirectory(shopDirectory);
            }

            return completeSuccess;
        }

        /// <summary>
        /// Загрузка папки для выбранного магазина.
        /// Имя магазина в базе должно совпадать с именем папки. Папка лежит в корне приложения в папке shops.
        /// Отдельно загружаются папки с брендами и категориями.
        /// 
        /// Возможные состояния:
        /// - магазин не создан
        /// - некорректная структура топономических папок (несовпадение количества папок или более/менее одной папки на одну топономическую единицу).
        /// - успешно обработано:
        ///   а) в некоторых карточках нет контента
        ///   б) ошибка сохранения одной или нескольких партий карточек в базу
        ///   в) п. а) и п. б) вместе
        ///   г) отсутствие проблем при обработке
        /// - нет брендов
        /// - нет категорий
        /// - нет всей топономии
        /// </summary>
        /// <param name="shopFolderName">Имя папки магазина</param>
        /// <returns>Успешно (частично или полностью) синхронизированный магазин</returns>
        public async Task<bool> SyncShopDirectory(string shopFolderName)
        {
            logger.WriteLine($"Sync shop: {shopFolderName}");

            var shop = await ReadOrCreateShop(shopFolderName);

            if (shop != null)
            {
                var shopFolderChildrenDirs = Directory.GetDirectories($"shops\\{shopFolderName}"); // TODO: path combine?

                logger.WriteLineIf(shopFolderChildrenDirs.Length != 2, $"Shop: {shop.Name}. WARNING!!! Count of subdirectories is {shopFolderChildrenDirs.Length}, but expected 2.");

                return await ProcessToponomies(shopFolderChildrenDirs, shop);
            }

            return false;
        }
        // @exceptionshandled

        #region Structural Methods

        private async Task<Shop?> ReadOrCreateShop(string shopName)
        {
            var result = await dataContext.Set<Shop>().FirstOrDefaultAsync(x => x.Name == shopName);
            if (result == null)
            {
                var shopToAdd = new Shop
                {
                    Name = shopName
                };
                dataContext.Set<Shop>().Add(shopToAdd);
                try
                {
                    await dataContext.SaveChangesAsync();
                    result = shopToAdd;
                }
                catch (Exception ex)
                {
                    logger.WriteLine($"Error saving new shop, processing stopped: {ex.Message}");
                }
            }

            return result;
        }
        // @exceptionshandled

        private async Task<bool> ProcessToponomies(string[] toponomyCandidateDirectories, Shop shop)
        {
            string? brandPath, categoryPath;
            try
            {
                brandPath = toponomyCandidateDirectories.SingleOrDefault(x => x.Contains("brands"));
                categoryPath = toponomyCandidateDirectories.SingleOrDefault(x => x.Contains("categories"));
            }
            catch (InvalidOperationException)
            {
                logger.WriteLine("Messy structure of toponomy directories: more than one brands and/or categories direcotry found. Processing stopped.");
                return false;
            }

            if (brandPath != null)
            {
                await ProcessToponomyRootPath(brandPath, dataContext, shop);
            }
            if (categoryPath != null)
            {
                await ProcessToponomyRootPath(categoryPath, dataContext, shop);
            }

            logger.WriteLineIf(brandPath != null && categoryPath != null, $"Shop: {shop.Name}. Normal processing.");
            logger.WriteLineIf(brandPath == null && categoryPath != null, $"Shop: {shop.Name}. Subdirectory with brands wasn't found, only partial processing would be performed.");
            logger.WriteLineIf(categoryPath == null && brandPath != null, $"Shop: {shop.Name}. Subdirectory with categories wasn't found, only partial processing would be performed.");
            logger.WriteLineIf(brandPath == null && categoryPath == null, $"Shop: {shop.Name}. WARNING: no subdirectory with brands and categories found. Processing skipped.");

            return true;
        }
        // @exceptionshandled



        // Загрузка папок с карточками товаров из файловой системы в базу
        private async Task ProcessToponomyRootPath(string toponomyRootPath, IntegratorDataContext dataContext, Shop shop)
        {
            var toponomyRootDirectory = new DirectoryInfo(toponomyRootPath);
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            DirectoryInfo shopRootDirectory = toponomyRootDirectory.Parent;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            var existingCardPaths = await dataContext.Set<Card>()
                .AsNoTracking()
                .Where(x => x.ShopId == shop.Id)
                .Select(x => x.FolderPath)
                .ToListAsync();

            var cardsToAdd = new List<Card>();
            const int cardsBatchSize = 100;
            foreach (DirectoryInfo cardDirectory in toponomyRootDirectory.GetDirectories("*", SearchOption.AllDirectories))
            {
                FileInfo? textFile;
                try
                {
                    textFile = cardDirectory.GetFiles("*.txt", SearchOption.TopDirectoryOnly).SingleOrDefault();
                }
                catch (InvalidOperationException)
                {
                    logger.WriteLine($"More than one text file in folder {cardDirectory.FullName}");
                    textFile = cardDirectory.GetFiles("*.txt", SearchOption.TopDirectoryOnly).First();
                }

                var cardToAdd = textFile != null;
                if (cardToAdd)
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    var card = await CreateCardFromDirectory(textFile, cardDirectory, shopRootDirectory, shop, existingCardPaths);
#pragma warning restore CS8604 // Possible null reference argument.
                    if (card != null)
                    {
                        cardsToAdd.Add(card);
                    }
                    else
                    {
                        cardToAdd = false;
                    }
                }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                logger.WriteLineIf(cardToAdd, $"directory {cardDirectory.Name}, text file '{textFile.Name}' was successfully processed. Card placed to list for addition.");
                logger.WriteLineIf(!cardToAdd, $"directory {cardDirectory.Name}, text file '{textFile?.Name ?? string.Empty}' wasn't found or has no content. Card skipped.");
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                if (cardsToAdd.Count >= cardsBatchSize)
                {
                    await ClearCardsBatch(cardsToAdd);
                }
            }
        }
        // @exceptionshandled



        private async Task<bool> ClearCardsBatch(List<Card> batch)
        {
            dataContext.Set<Card>().AddRange(batch);

            try
            {
                await dataContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                logger.WriteLine($"Oops, something wrong with adding cards to database. Please re-run current operation. Exception: {ex.GetType()} message: {ex.Message}");
                
                return false;
            }
            finally
            {
                batch.Clear();
            }
        }
        // @exceptionshandled

        private async Task<Card?> CreateCardFromDirectory(FileInfo textFile, DirectoryInfo cardDirectory, DirectoryInfo shopRootDirectory, Shop shop, List<string> existingCardPaths)
        {
#pragma warning disable CS8604 // Possible null reference argument.
            string content = await GetChineseTextFileContext(textFile) ?? string.Empty;
            var folderPath = GetCardRelativeToShopDirectoryPath(cardDirectory, shopRootDirectory);
#pragma warning restore CS8604 // Possible null reference argument.

            var cardToAdd = !string.IsNullOrEmpty(content) && !existingCardPaths.Contains(folderPath);
            if (cardToAdd)
            {
                var card = new Card
                {
                    FolderName = cardDirectory.Name,
                    FolderPath = folderPath,
                    TextFileName = textFile.Name,
                    TextFileContent = content,

                    InfoContent = StringHelper.RemoveExtraSymbols(content, "   |   "), // experimental field, can be safely removed

                    Shop = shop
                };

                var images = cardDirectory.GetFiles()
                    .Where(x => !x.Name.EndsWith(".txt"))
                    .Select(x => new CardImage
                    {
                        FolderName = cardDirectory.Name,
                        FolderPath = GetCardRelativeToShopDirectoryPath(cardDirectory, shopRootDirectory),
                        ImageFileName = x.Name,
                        ImageFileHash = null
                    })
                    .ToList();

                card.Images = images;

                return card;
            }

            return null;
        }
        // @exceptionshandled



        private async Task<string?> GetChineseTextFileContext(FileInfo textFile)
        {
            string? content = null;
            try
            {
                using var stream = textFile.OpenRead();
                using var textReader = new StreamReader(stream, Encoding.GetEncoding("gb2312"));
                content = await textReader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                logger.WriteLine($"Oops, something going wrong while reading text file: {textFile.FullName} exception: {ex.GetType()} message: {ex.Message}");
                content = null;
            }

            return content;
        }
        // @exceptionshandled

        #endregion

        #region Helper Methods

        private static string GetCardRelativeToShopDirectoryPath(DirectoryInfo cardDirectory, DirectoryInfo shopRootDirectory)
        {
            return cardDirectory.FullName.Replace(shopRootDirectory.FullName, string.Empty);
        }
        // @exceptionshandled

        #endregion
    }
}
