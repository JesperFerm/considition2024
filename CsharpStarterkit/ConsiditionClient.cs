using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpStarterkit
{
    public class ConsiditionClient
    {
        string gameUrl = "https://api.considition.com/";
        string apiKey = "9feda521-06ae-4f1a-8342-0e2554142ea7";
        HttpClient client = new();
        public async Task<GameOutput> SendToServer(GameInput input)
        {
            client.BaseAddress = new Uri(gameUrl, UriKind.Absolute);
            HttpRequestMessage request = new();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(gameUrl + "game", UriKind.Absolute);
            request.Headers.Add("x-api-key", apiKey);
            request.Content = new StringContent(JsonConvert.SerializeObject(input), Encoding.UTF8, "application/json");

            var res = client.Send(request);
            Console.WriteLine(res.StatusCode);
            Console.WriteLine(await res.Content.ReadAsStringAsync());

            return JsonConvert.DeserializeObject<GameOutput>(await res.Content.ReadAsStringAsync());
        }
    }
}
