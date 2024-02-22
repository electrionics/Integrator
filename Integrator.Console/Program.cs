// See https://aka.ms/new-console-template for more information
using Integrator.Data;
using Integrator.Data.Entities;
using Integrator.Data.Helpers;

using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Google.Cloud.Translate.V3;
using Google.Api.Gax.Grpc.Rest;
using Google.Api.Gax.ResourceNames;
using Grpc.Core;

//await Test();
//await CreateToponomyDrafts();
await ProcessDatabaseWithTemplates();
//await TranslateDatabase();
//await LoadShop("Магазин 1 - 千亿服饰3001档-№1-20240215T202614Z-001"); //  hanguang weiyishang xiezou xiezou2 千衣颂105A栋3楼330    号码YM22670449
return;

static async Task Test()
{
    string text = "𝐌𝐢𝐮𝐦𝐢 𝐱 𝐧𝐞𝐰 𝐁𝐚𝐥𝐚𝐧𝐜";
    foreach (char c in text)
    {
        int unicode = c;
        Console.WriteLine(unicode < 128 ? "ASCII: {0}" : "Non-ASCII: {0}", unicode);
    }

    Console.ReadLine();

    //using var dataContext = new IntegratorDataContext();
    //var items = (await dataContext.Set<Card>()
    //    .AsNoTracking()
    //    .Where(x => x.CardDetail.BrandId == null && x.CardDetail.CategoryId == null)
    //    .ToListAsync())
    //    .Select(x => new BrandDraft(x))
    //    .ToList();
}

#region Загрузка в базу папки для выбранного магазина

// Загрузка папки для выбранного магазина.
// Имя магазина в базе должно совпадать с именем папки. Папка лежит в корне приложения.
// Отдельно загружаются папки с брендами и категориями.
static async Task LoadShop(string shopName)
{
    Console.WriteLine($"Load shop: {shopName}");

    using var dataContext = GetDataContext();
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
static async Task ProcessRootDirectory(string rootDir, IntegratorDataContext dataContext, Shop shop)
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
                        //ImageFileHash = GetImageStreamHash(x)
                    })
                    .ToList();
                card.Images = images;

                dataContext.Set<Card>().Add(card);
            }
        }

        Console.WriteLine($"directory {directory.Name}, file {file?.Name ?? "-----"}");
    }

    await dataContext.SaveChangesAsync();
}

#endregion

#region Перевод текстов карточек магазина

static async Task TranslateDatabase()
{
    using var dataContext = GetDataContext();
    var shops = await dataContext.Set<Shop>().ToListAsync();

    foreach (var shop in shops)
    {
        await TranslateShop(shop.Name);
    }
}

// Переводит все тексты карточек товаров и относительные пути к папке карточки для выбранного магазина.
// Карточки, которые ранее были переведены, не переводятся повторно.
// Карточки с одинаковым текстом переводятся один раз.
static async Task TranslateShop(string shopName)
{
    Console.WriteLine($"Translate shop: {shopName}");

    #region Prepare to translate

    using var dataContext = GetDataContext();

    var shop = await dataContext.Set<Shop>().FirstAsync(x => x.Name == shopName);
    var cards = await dataContext.Set<Card>().Where(x => x.ShopId == shop.Id && x.CardTranslation == null).ToListAsync();

    var client = new TranslationServiceClientBuilder
    {
        GrpcAdapter = RestGrpcAdapter.Default,
    }.Build();
    //TranslationServiceClient client = TranslationServiceClient.Create();

    var translations = new Dictionary<int, CardTranslation>();
    var texts = new Dictionary<string, int>();

    foreach (var card in cards)
    {
        texts.TryAdd(card.TextFileContent, card.Id);
    }

    #endregion

    var sw = Stopwatch.StartNew();

    foreach (var card in cards)
    {
        var cardTranslation = new CardTranslation
        {
            CardId = card.Id,
        };

        // same text already translated for current batch
        if (translations.TryGetValue(texts[card.TextFileContent], out var existingTranslation))
        {
            cardTranslation.ContentEng = existingTranslation.ContentEng ?? string.Empty;
            cardTranslation.ContentRus = existingTranslation.ContentRus ?? string.Empty;

            try
            {
                var eng = await TranslateAsync(client, "en-US", card.FolderPath);
                var rus = await TranslateAsync(client, "ru-RU", card.FolderPath);

                cardTranslation.TitleEng = eng.path;
                cardTranslation.TitleRus = rus.path;

                translations.Add(card.Id, cardTranslation);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.ResourceExhausted)
            {
                await SaveAsync(dataContext, translations, sw);
                return;
            }
            catch (Exception)
            {
                await SaveAsync(dataContext, translations, sw);
                throw;
            }
        }
        else
        {
            try
            {
                var eng = await TranslateAsync(client, "en-US", card.FolderPath, card.TextFileContent);
                var rus = await TranslateAsync(client, "ru-RU", card.FolderPath, card.TextFileContent);

                cardTranslation.TitleEng = eng.path;
                cardTranslation.ContentEng = eng.text ?? string.Empty;
                cardTranslation.TitleRus = rus.path;
                cardTranslation.ContentRus = rus.text ?? string.Empty;

                translations.Add(card.Id, cardTranslation);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.ResourceExhausted)
            {
                await SaveAsync(dataContext, translations, sw);
                return;
            }
            catch (Exception)
            {
                await SaveAsync(dataContext, translations, sw);
                throw;
            }
        }
    }

    Console.WriteLine("No errors during translation!!!");
    await SaveAsync(dataContext, translations, sw);
}

static async Task SaveAsync(IntegratorDataContext dataContext, Dictionary<int, CardTranslation> translations, Stopwatch sw)
{
    sw.Stop();
    Console.WriteLine($"{sw.Elapsed} translations: {2 * translations.Select(x => x.Value.ContentEng).Distinct().Count()}");
    dataContext.Set<CardTranslation>().AddRange(translations.Select(x => x.Value));
    await dataContext.SaveChangesAsync();
}

static async Task<(string path, string text)> TranslateAsync(TranslationServiceClient client, string languageCode, string path, string? sourceText = null)
{
    TranslateTextRequest request;

    if (sourceText.IsNullOrEmpty())
    {
        request = new()
        {
            Contents = { path }
        };
    }
    else if (path.IsNullOrEmpty())
    {
        request = new()
        {
            Contents = { sourceText }
        };
    }
    else
    {
        request = new()
        {
            Contents = { path, sourceText }
        };
    }

    request.TargetLanguageCode = languageCode;
    request.Parent = new ProjectName("integrator-oleg-v1").ToString();

    var response = await client.TranslateTextAsync(request);

    (string path, string text) result;

    if (sourceText.IsNullOrEmpty())
    {
        result = new()
        {
            path = response.Translations[0].TranslatedText
        }; // response.Translations will have one entry, because request.Contents has one entry.
    }
    else if (path.IsNullOrEmpty())
    {
        result = new()
        {
            text = response.Translations[0].TranslatedText
        };
    }
    else
    {
        result = new()
        {
            path = response.Translations[0].TranslatedText,
            text = response.Translations[1].TranslatedText
        };
    }

    return result;
}

#endregion

#region Создание новых черновиков для категорий и брендов

static async Task CreateToponomyDrafts()
{
    Console.WriteLine($"Process Cards: category drafts and brand drafts");

    using var dataContext = GetDataContext();
    var categoryCandidates = (await dataContext.Set<Card>()
        .AsNoTracking()
        .ToListAsync())
        .GroupBy(x => StringHelper.GetParentFolder(x.FolderPath, x.FolderName))
        .Select(x => new CategoryDraft(x.First()))
        .Where(x => x.IsAcceptable)
        .ToList();

    var categoryDraftsExisting = await dataContext.Set<CategoryDraft>()
        .AsNoTracking()
        .Select(x => x.Name)
        .ToListAsync();

    var categoryDraftsToAdd = categoryCandidates
        .Where(x => !categoryDraftsExisting.Contains(x.Name))
        .ToList();

    await dataContext.Set<CategoryDraft>().AddRangeAsync(categoryDraftsToAdd);

    // same as category
    var brandCandidates = (await dataContext.Set<Card>()
        .AsNoTracking()
        .ToListAsync())
        .GroupBy(x => StringHelper.GetParentFolder(x.FolderPath, x.FolderName))
        .Select(x => new BrandDraft(x.First()))
        .Where(x => x.IsAcceptable)
        .ToList();

    var brandDraftsExisting = await dataContext.Set<BrandDraft>()
        .AsNoTracking()
        .Select(x => x.Name)
        .ToListAsync();

    var brandDraftsToAdd = brandCandidates
        .Where(x => !brandDraftsExisting.Contains(x.Name))
        .ToList();

    await dataContext.Set<BrandDraft>().AddRangeAsync(brandDraftsToAdd);

    await dataContext.SaveChangesAsync();
}

#endregion

#region Размечение брендов и категорий для карточек

// Размечение брендов и категорий для карточек исходя из путей и одинаковых текстов карточек.
static async Task MarkCardsWithToponomyItems()
{
    using var dataContext = GetDataContext();
    var cards = await dataContext.Set<Card>()
        .Include(x => x.CardDetail)
        .ToListAsync();

    var categoryDrafts = await dataContext.Set<CategoryDraft>()
        .AsNoTracking()
        .Include(x => x.Card)
        .Where(x => x.CategoryId != null)
        .ToListAsync();
    
    var brandDrafts = await dataContext.Set<BrandDraft>()
        .AsNoTracking()
        .Include(x => x.Card)
        .Where(x => x.BrandId != null)
        .ToListAsync();

    var counterBrands = 0;
    var counterCategories = 0;

    foreach (var card in cards)
    {
        if (card.CardDetail == null)
        {
            card.CardDetail = new();
        }
    }

    foreach (var draft in categoryDrafts)
    {
        foreach (var card in cards.Where(x => new CategoryDraft(x).Name == draft.Name))
        {
            card.CardDetail.CategoryId = draft.CategoryId;
            counterCategories++;
        }
    }
    foreach (var draft in brandDrafts)
    {
        foreach (var card in cards.Where(x => new BrandDraft(x).Name == draft.Name))
        {
            card.CardDetail.BrandId = draft.BrandId;
            counterBrands++;
        }
    }


    foreach (var draft in brandDrafts)
    {
        foreach (var card in cards.Where(x => x.CardDetail.BrandId == null && x.TextFileContent == draft.Card.TextFileContent))
        {
            card.CardDetail.BrandId = draft.BrandId;
            counterBrands++;
        }
    }
    foreach (var draft in categoryDrafts)
    {
        foreach (var card in cards.Where(x => x.CardDetail.CategoryId == null && x.TextFileContent == draft.Card.TextFileContent))
        {
            card.CardDetail.CategoryId = draft.CategoryId;
            counterCategories++;
        }
    }

    Console.WriteLine($"categories {counterCategories}, brands {counterBrands}");
    Console.ReadLine();

    await dataContext.SaveChangesAsync();
}

#endregion

#region Обработка карточек шаблонами

static async Task ProcessDatabaseWithTemplates()
{
    using var dataContext = GetDataContext();

    var templates = await dataContext.Set<Template>()
        .OrderBy(x => x.Order)
        .ToListAsync();

    var cards = await dataContext.Set<Card>()
        .Include(x => x.CardTranslation)
        .Include(x => x.CardDetail).ThenInclude(x => x.CardDetailSizes)
        .Include(x => x.CardDetail).ThenInclude(x => x.Brand)
        .Include(x => x.CardDetail).ThenInclude(x => x.TemplateMatches)
        .ToListAsync();

    var sizes = await dataContext.Set<Size>()
        .ToListAsync();

    var brands = await dataContext.Set<Brand>()
        .ToListAsync();

    var counter = 0;
    foreach (var template in templates)
    {
        foreach (var card in cards)
        {
            var processingResult = ProcessCardWithTemplate(card, template, sizes, brands);
            if (processingResult.matched)
            {
                counter++;
                var matching = new CardDetailTemplateMatch()
                {
                    CardDetailId = card.Id,
                    TemplateId = template.Id,

                    Value = processingResult.value
                };

                if (!card.CardDetail.TemplateMatches.Any(x =>
                    x.TemplateId == matching.TemplateId))
                {
                    SetTemplateMatchingValue(card, template, matching.Value);
                    dataContext.Set<CardDetailTemplateMatch>().Add(matching);
                }
                else
                {
                    Debug.WriteLine("Matching already processed.");
                }
            }
            if (counter > 0 && counter % 100 == 0)
            {
                counter++;
                await dataContext.SaveChangesAsync();
                Console.WriteLine($"Already matched: {counter}");
            }
        }
    }

    await dataContext.SaveChangesAsync();
    Console.WriteLine("Press any key");
    Console.ReadLine();
}

static (bool matched, string? value) ProcessCardWithTemplate(Card card, Template template, List<Size> allSizes, List<Brand> allBrands)
{
    bool isSuccess;

    Match match = null;
    var isRegex = template.Type == TemplateType.Price;

    if (isRegex)
    {
        Regex templateRegex = new(template.Search);
        match = templateRegex.Match(template.GetCardText(card));

        isSuccess = match.Success;
    }
    else
    {
        isSuccess = template
            .GetCardText(card)
            .Contains(template.Search, StringComparison.InvariantCultureIgnoreCase);
    }

    if (isSuccess)
    {
        switch (template.Type)
        {
            case TemplateType.Price:
                if (decimal.TryParse(match?.Groups[1].Value, out var price)) // 1 group must be here TODO: debug
                {
                    return (true, price.ToString());
                }
                else
                {
                    Debug.WriteLine(
                        $"Карточка {card.Id}. " +
                        $"Шаблон {template.Id}. " + 
                        $"Значение '{match.Groups[0].Value}' не подходит для цены.");

                    return (false, null);
                }
            case TemplateType.Brand:
                var newBrand = allBrands.FirstOrDefault(x => x.Name == template.Value);

                if (newBrand == null)
                {
                    Debug.WriteLine(
                        $"Карточка {card.Id}. " +
                        $"Шаблон {template.Id}. " +
                        $"Бренд с именем '{template.Value}' не найден в базе");
                    return (false, null);
                }

                return (true, newBrand.Id.ToString());
            case TemplateType.Color:
                return (true, template.Value);
                break;
            case TemplateType.Size:
                List<decimal> templateSetSizeValues;
                try
                {
                    templateSetSizeValues = template.Value
                        .Split(',')
                        .Select(decimal.Parse)
                        .ToList(); // semicolon-seperated values must be here
                }
                catch (FormatException)
                {
                    Debug.WriteLine(
                        $"Карточка {card.Id}. " +
                        $"Шаблон {template.Id}. " +
                        $"Размеры шаблона не могут быть распознаны: '{template.Value}'");
                    return (false, null);
                }

                var newSizes = allSizes
                    .Where(x => templateSetSizeValues.Contains(x.Value))
                    .Select(x => new CardDetailSize
                    {
                        SizeId = x.Id,
                        CardId = card.Id
                    })
                    .ToList();

                if (newSizes.Count == 0)
                {
                    return (false, null);
                }

                return (true, string.Join(',', newSizes.Select(x => x.SizeId)));
            default:
                return (false, null);
        }
    }

    return (false, null);
}

static void SetTemplateMatchingValue(Card card, Template template, string? matchingValue)
{
    if (!string.IsNullOrEmpty(matchingValue))
    {
        switch (template.Type)
        {
            case TemplateType.Price:

                var price = decimal.Parse(matchingValue);
                Debug.WriteLineIf(card.CardDetail.Price != null && card.CardDetail.Price != price,
                    $"Карточка {card.Id}. " +
                    $"Шаблон {template.Id}. " +
                    $"Значение цены {card.CardDetail.Price} перезаписано новым значением {price}.");

                card.CardDetail.Price = price;
                break;
            case TemplateType.Brand:
                var newBrandId = int.Parse(matchingValue);
                Debug.WriteLineIf(card.CardDetail.BrandId != null
                    && newBrandId != card.CardDetail.BrandId,
                        $"Карточка {card.Id}. " +
                        $"Шаблон {template.Id}. " +
                        $"Бренд '{card.CardDetail.BrandId}:{card.CardDetail.Brand.Name}' перезаписан новым значением '{newBrandId}:{template.Value}'");

                card.CardDetail.BrandId = newBrandId;
                break;
            case TemplateType.Color:
                Debug.WriteLineIf(card.CardDetail.Color != matchingValue,
                        $"Карточка {card.Id}. " +
                        $"Шаблон {template.Id}. " +
                        $"Цвет '{card.CardDetail.Color}' перезаписан новым значением '{matchingValue}'");

                card.CardDetail.Color = matchingValue;
                break;
            case TemplateType.Size:
                var newSizeIds = matchingValue
                    .Split(',')
                    .Select(int.Parse)
                    .ToList();
                var newSizes = newSizeIds.Select(x => new CardDetailSize
                {
                    SizeId = x,
                    CardId = card.Id
                });

                var beforeCount = card.CardDetail.CardDetailSizes.Count;
                var newCount = newSizeIds.Count;

                card.CardDetail.CardDetailSizes = card.CardDetail.CardDetailSizes
                    .UnionBy(newSizes, x => x.SizeId)
                    .ToList();

                var afterCount = card.CardDetail.CardDetailSizes.Count;

                Debug.WriteLineIf(beforeCount + newCount != afterCount,
                        $"Карточка {card.Id}. " +
                        $"Шаблон {template.Id}. " +
                        $"Размеры пересекаются! Количество размеров до изменения: {beforeCount}, новых: {newCount}, после изменения: {afterCount}");

                break;
            default:
                break;
        }
    }
}

#endregion



#region Вспомогательные методы

static IntegratorDataContext GetDataContext()
{
    return new IntegratorDataContext("server=.;database=Integrator;User Id=qqqq;Password=qqqq;TrustServerCertificate=True;");
}

static int GetImageStreamHash(FileInfo imageFile)
{
    return (new ImageHash().GetHash(imageFile.FullName)).GetHashCode();
}

#endregion