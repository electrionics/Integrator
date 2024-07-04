using System.Diagnostics;
using System.Drawing;
using System.Security.Cryptography;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Integrator.Data;
using Integrator.Data.Entities;
using Integrator.Shared;

namespace Integrator.Logic
{
    public class SameCardsLogic
    {
        private readonly ILogger<SameCardsLogic> logger;
        private readonly IntegratorDataContext dataContext;
        private readonly ApplicationConfig config;

        public SameCardsLogic(ILogger<SameCardsLogic> logger, IntegratorDataContext dataContext, ApplicationConfig config)
        {
            this.logger = logger;
            this.dataContext = dataContext;
            this.config = config;
        }

        public async Task MarkSameCards()
        {
            #region Compute Hash

            var images = await dataContext.Set<CardImage>()
                .Include(x => x.Card).ThenInclude(x => x.Shop)
                .Where(x => x.ImageFileHash == null || x.FileSizeBytes == null)
                .ToListAsync();

            var batchSize = 20;
            var counter = 0;
            foreach (var image in images)
            {
                var imagePath = GetFullPath(image);

                try
                {
                    var info = GetImageHash(imagePath);
                    image.ImageFileHash = info.hash;
                    image.FileSizeBytes = info.size;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Не удалось вычислить хэш и/или размер файла. Путь: {imagePath}");
                }

                counter++;
                await ClearHashesBatch(dataContext, batchSize, counter);
            }

            await dataContext.SaveChangesAsync();

            #endregion

            #region Compare Images

            images = await dataContext.Set<CardImage>().AsNoTracking()
                .Include(x => x.Card).ThenInclude(x => x.Detail).ThenInclude(x => x.Sizes)
                .Include(x => x.Card).ThenInclude(x => x.Shop)
                .Include(x => x.BaseImages)
                .Include(x => x.SimilarImages)
                .Where(x => x.ImageFileHash != null)
                .ToListAsync();

#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
#pragma warning disable CS8621 // Nullability of reference types in return type doesn't match the target delegate (possibly because of nullability attributes).
            var groupedImages = images
                .GroupBy(x => x.ImageFileHash)
                .Where(x => x.Count() > 1)
                .ToDictionary(x => x.Key, x => x.ToList());
#pragma warning restore CS8621 // Nullability of reference types in return type doesn't match the target delegate (possibly because of nullability attributes).
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.

            var sw = Stopwatch.StartNew();

            var imagesWhichEqual = groupedImages
                .AsParallel()
                .SelectMany(x => GetImagesWhichEqual(x.Value))
                .ToList();

            sw.Stop();
            logger.LogInformation($"Все картинки сравнены, результаты сравнений готовы. Время сравнения: {sw.Elapsed}");

            #endregion

            #region Save Similar Cards

            var similarCardsBatch = new List<CardSimilar>();
            const int similarCardsBatchSize = 100;
            sw.Restart();

            await dataContext.Set<CardSimilar>().ExecuteDeleteAsync();

            foreach (var imageSet in imagesWhichEqual) 
            { 
                foreach(var image in imageSet)
                {
                    if (image.Card.Detail == null)
                    {
                        image.Card.Detail = new() { CardId = image.CardId };
                    }
                }

                var imageList = imageSet.OrderByDescending(x => x.Card.Detail.Rating).ToList();

                var baseImage = imageList.First();
                var sameImages = imageList.Skip(1).ToList();

                foreach (var image in sameImages)
                {
                    var cardSimilar = new CardSimilar
                    {
                        BaseCardImageId = baseImage.Id,
                        SimilarCardImageId = image.Id,

                        BaseCardId = baseImage.CardId, // дано справочно
                        SimilarCardId = image.CardId, // дано справочно
                    };

                    similarCardsBatch.Add(cardSimilar);
                }

                if (similarCardsBatch.Count >= similarCardsBatchSize)
                {
                    await ClearSimilaritiesBatch(similarCardsBatch, sw);
                }
            }

            await ClearSimilaritiesBatch(similarCardsBatch, sw);

            #endregion
        }

        #region Structural Methods

        private static (string hash, long size) GetImageHash(string fileName)
        {
            using var md5 = MD5.Create();
            using var stream = new MemoryStream();
#pragma warning disable CA1416 // Validate platform compatibility
            using var image = System.Drawing.Image.FromFile(fileName);

            image.Save(stream, image.RawFormat);
#pragma warning restore CA1416 // Validate platform compatibility
            stream.Position = 0;
            
            var hash = md5.ComputeHash(stream);
            var convertedHash = BitConverter.ToString(hash).Replace("-", String.Empty).ToLowerInvariant();
            return (hash: convertedHash, size: stream.Length);
        }

        private List<List<CardImage>> GetImagesWhichEqual(List<CardImage> images)
        {
            var result = new List<List<CardImage>>();

            var stopwatch = Stopwatch.StartNew();

            for (var i = 0; i < images.Count; i++)
            {
                var baseImage = images[i];

                var imagesToCheck = images.Skip(i + 1);
                if (imagesToCheck.All(x => baseImage.SimilarImages.Select(y => y.SimilarCardImageId).Contains(x.Id)))
                {
                    result.Add(new() { baseImage });
                    result.First().AddRange(imagesToCheck);

                    return result;
                }

#pragma warning disable CA1416 // Validate platform compatibility
                using var img1 = new Bitmap(Image.FromFile(GetFullPath(baseImage)));

                foreach (var testImage in imagesToCheck)
                {
                    bool testImageEqual;
                    if (testImage.BaseImages.Any(x => x.BaseCardImageId == baseImage.Id))
                    {
                        testImageEqual = true;
                    }
                    else
                    {
                        using var img2 = new Bitmap(Image.FromFile(GetFullPath(testImage)));
                        testImageEqual = CompareImages(img1, img2);
                    }

                    if (testImageEqual)
                    {
                        var forAdding = result.FirstOrDefault();
                        if (forAdding == null)
                        {
                            forAdding = new List<CardImage> { baseImage };
                            result.Add(forAdding);
                        }

                        forAdding.Add(testImage);
                    }
#pragma warning restore CA1416 // Validate platform compatibility
                }

                if (result.Count > 0)
                {
                    break;
                }
            }

            stopwatch.Stop();
            logger.LogInformation($"Из {images.Count} картинок найдено {result.FirstOrDefault()?.Count ?? 0} одинаковых за {stopwatch.ElapsedMilliseconds} миллисекунд.");

            return result;
        }

        private bool CompareImages(Bitmap img1, Bitmap img2)
        {
            var result = true;
            var wrongCounter = 0;

#pragma warning disable CA1416 // Validate platform compatibility
            if (img1.Width == img2.Width && img1.Height == img2.Height)
            {
                for (int i = 0; i < img1.Width; i++)
                {
                    for (int j = 0; j < img1.Height; j++)
                    {

                        var img1_ref = img1.GetPixel(i, j);
                        var img2_ref = img2.GetPixel(i, j);

                        if (img1_ref != img2_ref)
                        {
                            result = false;
                            wrongCounter++;
                        }
                    }
                }
                if (!result)
                {
                    logger.LogInformation($"Image mismatches: {wrongCounter}");
                }
            }
            else
            {
                result = false;
            }
#pragma warning restore CA1416 // Validate platform compatibility

            return result;
        }

        #endregion


        #region Helper Methods

        private string GetFullPath(CardImage image)
        {
            return Path.Join(AppDomain.CurrentDomain.BaseDirectory, config.RootFolder, "shops", image.Card.Shop.Name, image.FolderPath, image.ImageFileName);
        }

        private async Task<bool> ClearSimilaritiesBatch(List<CardSimilar> batch, Stopwatch sw)
        {
            sw.Stop();
            logger.LogInformation($"Time spent: {sw.Elapsed} similarities: {batch.Count}");

            dataContext.Set<CardSimilar>().AddRange(batch);

            try
            {
                await dataContext.SaveChangesAsync();
                sw.Start();
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"Oops, something wrong with adding similarities to database. Please re-run current operation. Exception: {ex.GetType()} message: {ex.Message}");
                sw.Start();
                return false;
            }
            finally
            {
                batch.Clear();
            }
        }
        // @exceptionshandled

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataContext"></param>
        /// <param name="batchSize"></param>
        /// <param name="counter"></param>
        /// <returns>Boolean value, which determines if data context changes was saved</returns>
        private async Task<bool> ClearHashesBatch(IntegratorDataContext dataContext, int batchSize, int counter)
        {
            if (counter >= batchSize)
            {
                try
                {
                    await dataContext.SaveChangesAsync();
                    return true;
                }
                catch(Exception ex)
                {
                    logger.LogError(ex, "Error saving data context changes during images hash computing.");
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
