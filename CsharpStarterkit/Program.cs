using CsharpStarterkit;
using Newtonsoft.Json;

string gameUrl = "https://api.considition.com/";
string apiKey = "9feda521-06ae-4f1a-8342-0e2554142ea7";
string mapFile = "Map3.json";

ConsiditionClient considitionClient = new();
DockerClient dockerClient = new();



string mapDataText = File.ReadAllText(mapFile);
MapData mapData = JsonConvert.DeserializeObject<MapData>(mapDataText);
GameInput input = new()
{
    MapName = mapData.name,
    Proposals = new(),
    Iterations = new()
};
var loanAndAwardOptimizer = new LoanAndAwardOptimizer(mapData.gameLengthInMonths);
float budget = mapData.budget;

    // Define the personality ranking
Dictionary<string, int> personalityRank = new Dictionary<string, int>
    {
        { "RiskTaker", 1 },
        { "Spontaneous", 2 },
        { "Innovative", 3 },
        { "Practical", 4 },
        { "Conservative", 5 }
    };

// Order the customers
var orderedCustomers = mapData.customers
    .OrderByDescending(c => c.loan.amount)
    .Reverse()
    .ToList();


var testedCustomers = new List<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)>();
for (int i=0; i< orderedCustomers.Count; i++)
{
    var customer = orderedCustomers[i];
    Console.WriteLine($"Customer Nr: {i}");
    testedCustomers.Add(await loanAndAwardOptimizer.Optimize(customer, mapData.gameLengthInMonths, mapData.name));
}
var sorted = testedCustomers.ToList().OrderByDescending(c =>
{
    var customer = mapData.customers.FirstOrDefault(customer => customer.name == c.Item1.CustomerName);
    var awardCost = c.Item2.Sum(t => loanAndAwardOptimizer.CalculatePriceOfAward(customer, t[customer.name].Award, c.Item1));
    var cost = awardCost;
    var value = customer.loan.amount * (c.Item1.YearlyInterestRate/12) * c.Item1.MonthsToPayBackLoan;
    return c.Item3;
});

var optimalGroup = new OptimalGroup(sorted.ToList(), mapData.budget, mapData.name,mapData.gameLengthInMonths, mapData.customers.ToList());
var bestSelection = await optimalGroup.OptimizeSelection();
foreach (var kvp in bestSelection)
{
    var customer = mapData.customers.FirstOrDefault(c => c.name == kvp.Item1.CustomerName);
    var awardCost = kvp.Item2.Sum(t => loanAndAwardOptimizer.CalculatePriceOfAward(customer, t[customer.name].Award, kvp.Item1));
    var cost = customer.loan.amount + awardCost;
    if (budget > cost)
    {
        input.Proposals.Add(kvp.Item1);
        if (kvp.Item2.Any())
        {
            if (input.Iterations.Count == 0)
            {
                input.Iterations = kvp.Item2;
            }
            else
            {
                for (var index = 0; index < kvp.Item2.Count(); index++)
                {
                    input.Iterations[index][customer.name] = kvp.Item2[index][customer.name];
                }
            }

        }
        budget -= cost;
    }
}

considitionClient.SendToServer(input);