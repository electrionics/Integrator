using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Serilog;

using Integrator.Data;
using Integrator.Logic;

using Integrator.Web.Blazor.Server;
using Integrator.Web.Blazor.Shared;
using Integrator.Web.Blazor.Shared.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var databaseConfig = builder.Configuration.GetSection("Database").Get<DatabaseConfig>();
var applicationConfig = builder.Configuration.GetSection("Application").Get<ApplicationConfig>();

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

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);
builder.Host.UseSerilog(logger);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseSerilogRequestLogging();
app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
