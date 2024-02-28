// See https://aka.ms/new-console-template for more information

using Microsoft.EntityFrameworkCore;

using Integrator.Data;
using Integrator.Data.Entities;
using Integrator.Data.Helpers;
using Integrator.Shared;
using Integrator.Logic;

Console.WriteLine("Выберите процедуру для выполнения (1 - обработка шаблонами, 2 - перевод, пусто - выход, остальное - тест):");
switch (Console.ReadLine())
{
    case "1":
        await CallProcessDatabaseWithTemplates();
        break;
    case "2":
        await CallTranslateDatabase();
        break;
    case "3":
        Console.WriteLine("Имя магазина:");
#pragma warning disable CS8604 // Possible null reference argument.
        await CallLoadShop(shopName: Console.ReadLine());
#pragma warning restore CS8604 // Possible null reference argument.

        //  'hanguang'
        //  'weiyishang'
        //  'xiezou'
        //  'xiezou2'
        //  '千衣颂105A栋3楼330    号码YM22670449'
        //  'Магазин 1 - 千亿服饰3001档-№1-20240215T202614Z-001'

        break;
    case "4":

        break;
    case "":
        return;
    default:
        await Test();
        break;
}

await CreateToponomyDrafts();
                    
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

// следующие два региона надо заменить на логику шаблонов в будущем

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


static async Task CallLoadShop(string shopName)
{
    var logic = new ShopDirectoryLogic(GetDataContext(), GetLoggerDecorator());

    await logic.LoadShop(shopName);

    Console.WriteLine("Press any key");
    Console.ReadLine();
}

static async Task CallProcessDatabaseWithTemplates()
{
    var logic = new TemplateLogic(GetDataContext(), GetLoggerDecorator());

    await logic.ProcessDatabaseWithTemplates();

    Console.WriteLine("Press any key");
    Console.ReadLine();
}

static async Task CallTranslateDatabase()
{
    var logic = new TranslateLogic(GetDataContext(), GetLoggerDecorator());

    await logic.TranslateDatabase();

    Console.WriteLine("Press any key");
    Console.ReadLine();
}

#region Вспомогательные методы

static IntegratorDataContext GetDataContext()
{
    return new IntegratorDataContext("server=.;database=Integrator;User Id=qqqq;Password=qqqq;TrustServerCertificate=True;");
}

static ILogger GetLoggerDecorator()
{
    return new LoggerDecorator(new Integrator.Console.ConsoleLogger(), new DebugLogger());
}

static int GetImageStreamHash(FileInfo imageFile)
{
    return (new ImageHash().GetHash(imageFile.FullName)).GetHashCode();
}

#endregion