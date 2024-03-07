// See https://aka.ms/new-console-template for more information

using Integrator.Data;
using Integrator.Data.Helpers;
using Integrator.Shared;
using Integrator.Logic;

// Магазины:
//  'hanguang'
//  'weiyishang'
//  'xiezou'
//  'xiezou2'
//  '千衣颂105A栋3楼330    号码YM22670449'
//  'Магазин 1 - 千亿服饰3001档-№1-20240215T202614Z-001'


Console.WriteLine("Выберите процедуру для выполнения:\n" +
    "1 - обработка шаблонами, \n" +
    "2 - перевод, \n" +
    "3 - загрузка карточек магазина в БД,\n" +
    "4 - создание новых черновиков для брендов и категорий,\n" +
    "5 - первичная разметка карточек брендами и категориями,\n" +
    "\n" +
    "пусто - выход\n, " +
    "остальное - тест.\n");
Console.Write("Мой выбор: ");
var userValue = Console.ReadLine();
switch (userValue)
{
    case "1":
        WriteUserChoose(userValue, "обработка шаблонами");
        await CallProcessDatabaseWithTemplates();
        break;
    case "2":
        WriteUserChoose(userValue, "перевод");
        await CallTranslateDatabase();
        break;
    case "3":
        WriteUserChoose(userValue, "загрузка карточек магазина в БД");
        Console.WriteLine("Имя магазина:");
        await CallLoadShop(shopName: Console.ReadLine() ?? string.Empty);
        break;
    case "4":
        WriteUserChoose(userValue, "создание новых черновиков для брендов и категорий");
        await CallCreateToponomyDrafts();
        break;
    case "5":
        WriteUserChoose(userValue, "первичная разметка карточек брендами и категориями");
        await CallMarkCardsWithToponomyItems();
        break;
    case "":
        WriteUserChoose(userValue, "выход");
        return;
    default:
        WriteUserChoose(userValue ?? "", "тест");
        await CallTest();
        break;
}
                    
return;

static void WriteUserChoose(string userValue, string procedureName)
{
    Console.WriteLine($"Выбрано значение: {userValue}. Запущена процедура '{procedureName}'.");
}

static async Task CallTest()
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

    await Task.CompletedTask;
}


#region Вызовы логики по обработке данных

static async Task CallCreateToponomyDrafts()
{
    var logic = new ToponomyLogic(GetDataContext(), GetLoggerDecorator());

    await logic.CreateToponomyDrafts();

    Console.WriteLine("Press any key");
    Console.ReadLine();
}

static async Task CallMarkCardsWithToponomyItems()
{
    var logic = new ToponomyLogic(GetDataContext(), GetLoggerDecorator());

    await logic.MarkCardsWithToponomyItems();

    Console.WriteLine("Press any key");
    Console.ReadLine();
}

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

#endregion


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