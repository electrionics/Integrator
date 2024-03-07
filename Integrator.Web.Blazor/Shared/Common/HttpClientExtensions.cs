using Newtonsoft.Json;
using System.Net.Http.Json;

namespace Integrator.Web.Blazor.Shared.Common
{
    public static class HttpClientExtensions
    {
        public static async Task<TResult?> PostResultAsJsonAsync<TResult, TBody>(this HttpClient client, string url, TBody body, bool ensureSuccess = true)
        {
            var result = await client.PostAsync(url, JsonContent.Create(body));
            if (ensureSuccess)
            {
                result.EnsureSuccessStatusCode();
            }
            var jsonStr = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TResult>(jsonStr);
        }
    }
}
