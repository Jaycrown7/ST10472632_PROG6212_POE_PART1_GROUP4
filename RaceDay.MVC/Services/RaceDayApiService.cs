using System.Net.Http.Json;

namespace RaceDay.MVC.Services
{
    public class RaceDayApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RaceDayApiService(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        private HttpClient CreateClient()
        {
            return _httpClientFactory.CreateClient("RaceDayApi");
        }

        private ISession Session =>
            _httpContextAccessor.HttpContext!.Session;

        public async Task<HttpResponseMessage> RegisterAsync<T>(T request)
        {
            var client = CreateClient();

            return await client.PostAsJsonAsync(
                "api/auth/register",
                request);
        }
        public async Task<HttpResponseMessage> LoginAsync<T>(T request)
        {
            var client = CreateClient();

            var response = await client.PostAsJsonAsync(
                "api/auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                if (response.Headers.TryGetValues(
                    "Set-Cookie", out var cookies))
                {
                    var authCookie = cookies.FirstOrDefault(
                        cookie => cookie.StartsWith(
                            "RaceDay.Auth=",
                            StringComparison.OrdinalIgnoreCase));

                    if (authCookie != null)
                    {
                        Session.SetString(
                            "ApiAuthCookie",
                            authCookie.Split(';')[0]);
                    }
                }
            }

            return response;
        }

        public async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string endpoint,
            object? body = null)
        {
            var client = CreateClient();

            var request = new HttpRequestMessage(method, endpoint);

            var authCookie = Session.GetString("ApiAuthCookie");

            if (!string.IsNullOrEmpty(authCookie))
            {
                request.Headers.Add("Cookie", authCookie);
            }

            if (body != null)
            {
                request.Content = JsonContent.Create(body);
            }

            return await client.SendAsync(request);
        }

        public async Task LogoutAsync()
        {
            try
            {
                await SendAsync(
                    HttpMethod.Post,
                    "api/auth/logout");
            }
            finally
            {
                Session.Clear();
            }
        }
    }
}