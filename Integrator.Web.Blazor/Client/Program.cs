using Integrator.Web.Blazor.Client;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;
using Syncfusion.Blazor.Popups;

// alena (v24): Ngo9BigBOggjHTQxAR8/V1NAaF5cWWJCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWXxednRRRmVcVkJ2V0I=
// old trial: MzA4NTYxNUAzMjM0MmUzMDJlMzBpeGxzb082cEZHalkwdGl1UE5aU25lcGI5R1RqYW5HRG1WTks1anFjN1JRPQ==
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NBaF5cXmZCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWXtfcHRRR2deV0JyW0c="); // v25

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSyncfusionBlazor();
builder.Services.AddScoped<SfDialogService>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress), Timeout = TimeSpan.FromMinutes(60) });

await builder.Build().RunAsync();
