using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpStarterkit
{
    public class DockerClient
    {
        string gameUrl = "http://localhost:8080/";
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
            var content = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
            {
                return new GameOutput() { score = new Score { environmentalImpact = int.MinValue, happinessScore = int.MinValue, totalProfit = int.MinValue, totalScore = int.MinValue } };
            }
            return JsonConvert.DeserializeObject<GameOutput>(content);
        }
    }
}
