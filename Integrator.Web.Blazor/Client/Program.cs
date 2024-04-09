using Integrator.Web.Blazor.Client;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;
using Syncfusion.Blazor.Popups;

// alena: Ngo9BigBOggjHTQxAR8/V1NAaF5cWWJCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWXxednRRRmVcVkJ2V0I=
// old trial: MzA4NTYxNUAzMjM0MmUzMDJlMzBpeGxzb082cEZHalkwdGl1UE5aU25lcGI5R1RqYW5HRG1WTks1anFjN1JRPQ==
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NAaF5cWWJCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWXxednRRRmVcVkJ2V0I=");

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSyncfusionBlazor();
builder.Services.AddScoped<SfDialogService>();
builder.Services.AddScoped(sp => new HttpClient { MaxResponseContentBufferSize = 100000000, BaseAddress = new Uri(builder.HostEnvironment.BaseAddress), Timeout = TimeSpan.FromSeconds(60) });

await builder.Build().RunAsync();
