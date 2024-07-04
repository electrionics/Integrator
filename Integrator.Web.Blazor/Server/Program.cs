using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Timeouts;

using FluentValidation;
using Serilog;

using Integrator.Data;
using Integrator.Logic;
using Integrator.Shared;
using Integrator.Web.Blazor.Server;
using Integrator.Web.Blazor.Shared;
using Integrator.Web.Blazor.Shared.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var databaseConfig = builder.Configuration.GetSection("Database").Get<DatabaseConfig>();
var applicationConfig = builder.Configuration.GetSection("Application").Get<ApplicationConfig>();

builder.Services.AddRequestTimeouts((options) =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    options.AddPolicy(ServerConstants.LongRunningPolicyName, TimeSpan.FromMinutes(60));
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<IntegratorDataContext>((options) =>
{
    options.UseSqlServer(databaseConfig?.ConnectionString);
});

builder.Services.AddSingleton(applicationConfig!);

builder.Services.AddScoped<IValidator<TemplateEditViewModel>, TemplateEditViewModelValidator>();
builder.Services.AddScoped<IValidator<ReplacementEditViewModel>, ReplacementEditViewModelValidator>();

builder.Services.AddScoped<ShopDirectoryLogic>();
builder.Services.AddScoped<TranslateLogic>();
builder.Services.AddScoped<TemplateLogic>();
builder.Services.AddScoped<ReplacementLogic>();
builder.Services.AddScoped<SameCardsLogic>();
builder.Services.AddScoped<ExportLogic>();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);
builder.Host.UseSerilog(logger);
//builder.Services.AddResponseCompression((options) =>
//{
//    options.EnableForHttps = true;
//}); // TODO: see below

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    //app.UseResponseCompression(); //TODO: try enable and test performance
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseRequestTimeouts();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
