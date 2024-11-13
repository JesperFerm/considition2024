using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpStarterkit
{
    public class OptimalGroup
    {
        private readonly List<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)> proposals;
        private readonly float budget;
        private readonly string mapName;
        private readonly int populationSize = 100;
        private readonly List<Customer> customers;
        private readonly int generations = 10;
        private readonly Random random = new Random();
        private readonly LoanAndAwardOptimizer loanAndAwardOptimizer;

        public OptimalGroup(List<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)> proposals, float budget, string mapName, int gameLength, List<Customer> customers)
        {
            this.proposals = proposals;
            this.budget = budget;
            this.mapName = mapName;
            this.loanAndAwardOptimizer = new LoanAndAwardOptimizer(gameLength);
            this.customers = customers;
        }

        public async Task<List<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)>> OptimizeSelection()
        {
            var population = InitializePopulation();

            for (int generation = 0; generation < generations; generation++)
            {
                // Run CalculateScore asynchronously for each selection in the population
                var scoredTasks = population.Select(async selection =>
                {
                    var wow = (selection, score: await CalculateScore(selection, mapName), cost: CalculateCost(selection));
                    return wow;
                }).ToList();

                // Await all scoring tasks to complete
                var scoredPopulation = (await Task.WhenAll(scoredTasks))
                    .Where(x => x.cost <= budget)
                    .OrderByDescending(x => x.score)
                    .ToList();

                Console.WriteLine($"Generation: {generation + 1} NumberOfSugestions within budget: {scoredPopulation.Count}, bestScore: {scoredPopulation.FirstOrDefault().score}");

                if (scoredPopulation.Count == 0) break;

                population = EvolvePopulation(scoredPopulation.ToList());
            }

            // Find the best selection after the last generation
            var finalPopulation = await Task.WhenAll(population.Select(async p =>
                (selection: p, score: await CalculateScore(p, mapName), cost: CalculateCost(p))));

            return finalPopulation
                .Where(x => x.cost <= budget)
                .OrderByDescending(x => x.score)
                .Select(x => x.selection)
                .FirstOrDefault();
        }


        private List<List<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)>> InitializePopulation()
        {
            var population = new List<List<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)>>();

            for (int i = 0; i < populationSize; i++)
            {
                var selection = proposals.Where(p => p.Item3 > 0 && random.NextDouble() > 0.5).ToList();
                population.Add(selection);
            }

            return population;
        }
        private async Task<float> CalculateScore(List<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)> selection, string mapName)
        {
            var client = new DockerClient();
            var thisBudget = budget;
            var input = new GameInput
            {
                MapName = mapName,
                Proposals = new List<CustomerLoanRequestProposal>(),
                Iterations = new List<Dictionary<string, CustomerAction>>()
            };

            // Initialize `input.Iterations` with an empty dictionary for each month
            int gameLength = selection.First().Item2.Count;
            for (int month = 0; month < gameLength; month++)
            {
                input.Iterations.Add(new Dictionary<string, CustomerAction>());
            }

            // Populate `input.Proposals` and `input.Iterations` based on selected proposals
            foreach (var kvp in selection)
            {
                var customer = customers.FirstOrDefault(c => c.name == kvp.Item1.CustomerName);
                if (customer == null) continue; // Ensure the customer exists

                var awardCost = kvp.Item2.Sum(t => loanAndAwardOptimizer.CalculatePriceOfAward(customer, t[customer.name].Award, kvp.Item1));
                var cost = customer.loan.amount + awardCost;

                if (thisBudget > cost)
                {
                    input.Proposals.Add(kvp.Item1);

                    // Add actions for the customer to `input.Iterations`, avoiding overwrites
                    for (int index = 0; index < kvp.Item2.Count; index++)
                    {
                        if (kvp.Item2[index].ContainsKey(customer.name))
                        {
                            // Only add the action if it doesn't already exist for this customer in this month
                            if (!input.Iterations[index].ContainsKey(customer.name))
                            {
                                input.Iterations[index][customer.name] = kvp.Item2[index][customer.name];
                            }
                        }
                    }

                    thisBudget -= cost;
                }
            }

            var ret = await client.SendToServer(input);
            float score = ret.score.totalScore;
            return ret.score.happinessScore;
        }

        private float CalculateCost(List<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)> selection)
        {
            return selection.Sum(x => x.Item2.Sum(action => action.Values.Sum(a => GetActionCost(a))));
        }

        private float GetActionCost(CustomerAction action)
        {
            return float.TryParse(action.Award, out float cost) ? cost : 0.0f;
        }

        private List<List<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)>> EvolvePopulation(
            List<(List<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)> selection, float score, float cost)> scoredPopulation)
        {
            var newPopulation = new List<List<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)>>();

            // Select top 20% for crossover
            var topPerformers = scoredPopulation.Take(populationSize / 5).ToList();

            // Add top performers to the new population directly
            newPopulation.AddRange(topPerformers.Select(x => x.selection));

            // Create new individuals through crossover and mutation
            while (newPopulation.Count < populationSize)
            {
                var parent1 = topPerformers[random.Next(topPerformers.Count)].selection;
                var parent2 = topPerformers[random.Next(topPerformers.Count)].selection;
                var child = Crossover(parent1, parent2);
                child = Mutation(child);
                newPopulation.Add(child);
            }

            return newPopulation;
        }

        private List<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)> Crossover(
            List<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)> parent1,
            List<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)> parent2)
        {
            var child = new List<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)>();

            for (int i = 0; i < parent1.Count; i++)
            {
                if (i < parent2.Count && random.NextDouble() > 0.5)
                {
                    child.Add(parent2[i]);
                }
                else
                {
                    child.Add(parent1[i]);
                }
            }

            return child.Distinct().ToList();
        }

        private List<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)> Mutation(List<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)> individual)
        {
            if (random.NextDouble() > 0.5) // 20% chance of mutation
            {
                return proposals.Where(_ => random.NextDouble() > 0.5).ToList();
            }
            return individual;
        }
    }
}
