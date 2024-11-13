namespace CsharpStarterkit
{
    //{"gameId":"c815bf56-7dea-4925-9771-3abf96331820","score":{"totalProfit":164459.990104,"happynessScore":-5820,"environmentalImpact":200,"totalScore":158839.990104,"mapName":"Gothenburg"},"message":null,"achievementsUnlocked":[]}
    public class GameOutput
    {
        public string gameId { get; set; }
        public Score score { get; set; }
        public string name { get; set; }
        public string message { get; set; }
        public List<string> achievementsUnlocked { get; set; }
    }
    public class Score
    {
        public float totalProfit { get; set; }
        public float totalScore { get; set; }
        public float happinessScore { get; set; }
        public int environmentalImpact { get; set; }
        public string mapName { get; set; }
    }
}
