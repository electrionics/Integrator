using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Integrator.Web.Blazor.Client.Authentication
{
    public class CookieDelegatingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
