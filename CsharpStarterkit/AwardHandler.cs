using CsharpStarterkit;


namespace CsharpStarterkit
{
    public class LoanAndAwardOptimizer
    {
        public LoanAndAwardOptimizer(int gamelength) {
            possibleMonthValues = new List<int>() { gamelength / 8, gamelength / 6, gamelength / 4, gamelength / 3, gamelength / 2, gamelength, gamelength * 2, gamelength * 3, gamelength * 4 };
        }
        // Genetic algorithm parameters
        private int populationSize = 50;
        private int generations = 100;
        private float crossoverRate = 0.7f;
        private float mutationRate = 0.3f;
        private Random random = new Random();

        // Bounds for loan parameters
        private float lowerBoundInterest = 0.0005f;
        private float upperBoundInterest = 1;
        private List<int> possibleMonthValues = new List<int> { 6, 12, 18, 24, 36, 60 };

        // List of awards
        //I missed that theese were hidden on final day...
        public List<Award> awards = new List<Award>
    {
        new Award { cost = 0, baseHappiness = 0, name = "None" },
        new Award { cost = 1500, baseHappiness = 15, name = "IkeaFoodCoupon" },
        new Award { cost = 2000, baseHappiness = 25, name = "IkeaDeliveryCheck" },
        new Award { cost = 300, baseHappiness = 40, name = "IkeaCheck" },
        new Award { cost = 5000, baseHappiness = 75, name = "GiftCard" },
        new Award { cost = 15000.0f, baseHappiness = 250, name = "HalfInterestRate" },
        new Award { cost = 15000.0f, baseHappiness = 500, name = "NoInterestRate" }
    };

        // Class representing an individual in the population
        public class Individual
        {
            public CustomerLoanRequestProposal LoanProposal { get; set; }
            public List<string> AwardSequence { get; set; }
        }
        public float CalculatePriceOfAward(Customer customer, string award, CustomerLoanRequestProposal proposal)
        {
            var awardObj = awards.Find(a=> a.name == award);
            var cost = awardObj.cost;
            if(awardObj.name == "NoInterestRate" )
            {
                cost += customer.loan.amount * (proposal.YearlyInterestRate / 12);
            }
            if(awardObj.name == "HalfInterestRate")
            {
                cost += (customer.loan.amount * (proposal.YearlyInterestRate / 12)) / 2;
            }
            return cost;

        }
        public async Task<(CustomerLoanRequestProposal, List<Dictionary<string, CustomerAction>>, float)> Optimize(Customer customer, int gameLength, string mapName)
        {
            var population = InitializePopulation(customer, gameLength);

            for (int generation = 0; generation < generations; generation++)
            {
                var fitnessScores = await EvaluateFitness(population, customer, gameLength, mapName);

                // Optional: Output progress
                Console.WriteLine($"Generation: {generation + 1} for customer: {customer.name}, Best Fitness = {fitnessScores.Max()}");

                var selectedIndividuals = Selection(population, fitnessScores);
                var offspring = Crossover(selectedIndividuals);
                var mutatedOffspring = Mutation(offspring);

                population = mutatedOffspring;
            }

            // Get the best individual
            var finalFitnessScores = await EvaluateFitness(population, customer, gameLength, mapName);
            var bestIndex = finalFitnessScores.IndexOf(finalFitnessScores.Max());
            var bestIndividual = population[bestIndex];

            var iterations = ConvertToIterations(customer, bestIndividual.AwardSequence, bestIndividual.LoanProposal);

            return (bestIndividual.LoanProposal, iterations, finalFitnessScores.Max());
        }

        private List<Individual> InitializePopulation(Customer customer, int gameLength)
        {
            List<Individual> population = new List<Individual>();

            for (int i = 0; i < populationSize; i++)
            {
                // Randomly select loan parameters
                float interestRate = (float)(lowerBoundInterest + random.NextDouble() * (upperBoundInterest - lowerBoundInterest));
                int monthsToPayBackLoan = possibleMonthValues[random.Next(possibleMonthValues.Count)];

                var loanProposal = new CustomerLoanRequestProposal
                {
                    CustomerName = customer.name,
                    YearlyInterestRate = interestRate,
                    MonthsToPayBackLoan = monthsToPayBackLoan
                };

                // Randomly generate award sequence 
                List<string> awardSequence = new List<string>();
                for (int j = 0; j < gameLength; j++)
                {
                    int awardIndex = random.Next(awards.Count);
                    awardSequence.Add(awards[awardIndex].name);
                    
                }

                var individual = new Individual
                {
                    LoanProposal = loanProposal,
                    AwardSequence = awardSequence
                };

                population.Add(individual);
            }

            return population;
        }

        private async Task<List<float>> EvaluateFitness(List<Individual> population, Customer customer, int gameLength, string mapName)
        {
            List<float> fitnessScores = new List<float>();

            foreach (var individual in population)
            {
                var iterations = ConvertToIterations(customer, individual.AwardSequence, individual.LoanProposal);
                float fitness = await GetHappinessScore(customer, gameLength, iterations, individual.LoanProposal, mapName);
                fitnessScores.Add(fitness);
            }

            return fitnessScores;
        }

        private List<Individual> Selection(List<Individual> population, List<float> fitnessScores)
        {
            List<Individual> selected = new List<Individual>();

            int tournamentSize = 10;
            for (int i = 0; i < populationSize; i++)
            {
                List<int> indices = new List<int>();
                for (int j = 0; j < tournamentSize; j++)
                {
                    indices.Add(random.Next(populationSize));
                }
                float bestFitness = float.MinValue;
                int bestIndex = -1;
                foreach (int index in indices)
                {
                    if (fitnessScores[index] > bestFitness)
                    {
                        bestFitness = fitnessScores[index];
                        bestIndex = index;
                    }
                }
                selected.Add(population[bestIndex]);
            }

            return selected;
        }

        private List<Individual> Crossover(List<Individual> selectedIndividuals)
        {
            List<Individual> offspring = new List<Individual>();

            for (int i = 0; i < selectedIndividuals.Count; i += 2)
            {
                Individual parent1 = selectedIndividuals[i];
                Individual parent2 = selectedIndividuals[(i + 1) % selectedIndividuals.Count];

                if (random.NextDouble() < crossoverRate)
                {
                    // Crossover loan parameters
                    float alpha = (float)random.NextDouble();

                    float interestRateChild1 = alpha * parent1.LoanProposal.YearlyInterestRate + (1 - alpha) * parent2.LoanProposal.YearlyInterestRate;
                    float interestRateChild2 = (1 - alpha) * parent1.LoanProposal.YearlyInterestRate + alpha * parent2.LoanProposal.YearlyInterestRate;

                    // For MonthsToPayBackLoan, randomly select from parents
                    int monthsChild1 = random.NextDouble() < 0.5 ? parent1.LoanProposal.MonthsToPayBackLoan : parent2.LoanProposal.MonthsToPayBackLoan;
                    int monthsChild2 = random.NextDouble() < 0.5 ? parent1.LoanProposal.MonthsToPayBackLoan : parent2.LoanProposal.MonthsToPayBackLoan;

                    var loanChild1 = new CustomerLoanRequestProposal
                    {
                        CustomerName = parent1.LoanProposal.CustomerName,
                        YearlyInterestRate = interestRateChild1,
                        MonthsToPayBackLoan = monthsChild1
                    };

                    var loanChild2 = new CustomerLoanRequestProposal
                    {
                        CustomerName = parent2.LoanProposal.CustomerName,
                        YearlyInterestRate = interestRateChild2,
                        MonthsToPayBackLoan = monthsChild2
                    };

                    // Uniform crossover for award sequences
                    var awardSequenceChild1 = new List<string>();
                    var awardSequenceChild2 = new List<string>();
                    for (int j = 0; j < parent1.AwardSequence.Count; j++)
                    {
                        if (random.NextDouble() < 0.5)
                        {
                            awardSequenceChild1.Add(parent1.AwardSequence[j]);
                            awardSequenceChild2.Add(parent2.AwardSequence[j]);
                        }
                        else
                        {
                            awardSequenceChild1.Add(parent2.AwardSequence[j]);
                            awardSequenceChild2.Add(parent1.AwardSequence[j]);
                        }
                    }

                    var child1 = new Individual
                    {
                        LoanProposal = Clamp(loanChild1),
                        AwardSequence = awardSequenceChild1
                    };

                    var child2 = new Individual
                    {
                        LoanProposal = Clamp(loanChild2),
                        AwardSequence = awardSequenceChild2
                    };

                    offspring.Add(child1);
                    offspring.Add(child2);
                }
                else
                {
                    offspring.Add(parent1);
                    offspring.Add(parent2);
                }
            }

            return offspring;
        }


        private List<Individual> Mutation(List<Individual> offspring)
        {
            List<Individual> mutatedOffspring = new List<Individual>();

            foreach (var individual in offspring)
            {
                var mutatedIndividual = new Individual
                {
                    LoanProposal = individual.LoanProposal,
                    AwardSequence = new List<string>(individual.AwardSequence)
                };

                if (random.NextDouble() < mutationRate)
                {
                    // Mutate YearlyInterestRate
                    float mutationAmountInterest = (float)((random.NextDouble() * 2 - 1) * (upperBoundInterest - lowerBoundInterest) * 0.05);
                    mutatedIndividual.LoanProposal.YearlyInterestRate += mutationAmountInterest;
                    mutatedIndividual.LoanProposal.YearlyInterestRate = Math.Max(lowerBoundInterest, Math.Min(upperBoundInterest, mutatedIndividual.LoanProposal.YearlyInterestRate));
                    // Mutate MonthsToPayBackLoan
                   
                }
                else
                {
                    if (random.NextDouble() < mutationRate)
                    {
                        // Randomly select a new months value
                        mutatedIndividual.LoanProposal.MonthsToPayBackLoan = possibleMonthValues[random.Next(possibleMonthValues.Count)];
                    }
                }

                // Mutate award sequence
                for (int i = 0; i < mutatedIndividual.AwardSequence.Count; i++)
                {
                        if (random.NextDouble() < mutationRate)
                        {
                            // Mutate by changing the award at this position
                            int awardIndex = random.Next(awards.Count);
                            mutatedIndividual.AwardSequence[i] = awards[awardIndex].name;
                        }
                    
                }

                // Ensure awards after loan term are "None"
                mutatedIndividual.AwardSequence = mutatedIndividual.AwardSequence;

                mutatedOffspring.Add(mutatedIndividual);
            }

            return mutatedOffspring;
        }

        private CustomerLoanRequestProposal Clamp(CustomerLoanRequestProposal loan)
        {
            return new CustomerLoanRequestProposal
            {
                CustomerName = loan.CustomerName,
                YearlyInterestRate = Math.Max(lowerBoundInterest, Math.Min(upperBoundInterest, loan.YearlyInterestRate)),
                MonthsToPayBackLoan = loan.MonthsToPayBackLoan,
            };
        }

        private List<string> AdjustAwardSequence(List<string> awardSequence, int monthsToPayBackLoan)
        {
            for (int i = 0; i < awardSequence.Count; i++)
            {
                if (i > monthsToPayBackLoan)
                {
                    awardSequence[i] = "None";
                }
            }
            return awardSequence;
        }

        private List<Dictionary<string, CustomerAction>> ConvertToIterations(Customer customer, List<string> awardSequence, CustomerLoanRequestProposal proposal)
        {
            List<Dictionary<string, CustomerAction>> iterations = new List<Dictionary<string, CustomerAction>>();

            for(var i = 0; i < awardSequence.Count(); i++)
            {
                var awardName = awardSequence[i];
                Dictionary<string, CustomerAction> actionDict = new Dictionary<string, CustomerAction>
            {
                {
                    customer.name,
                    new CustomerAction
                    {
                        Type = awardName == "None" ? "Skip" : "Award",
                        Award = awardName
                    }
                }
            };
                iterations.Add(actionDict);
            }

            return iterations;
        }

        public async Task<float> GetHappinessScore(Customer customer, int gameLength, List<Dictionary<string, CustomerAction>> iterations, CustomerLoanRequestProposal loan, string mapName)
        {
            var client = new DockerClient();
            var input = new GameInput
            {
                MapName = mapName,
                Proposals = new List<CustomerLoanRequestProposal>
                {
                    new CustomerLoanRequestProposal
                    {
                        CustomerName = customer.name,
                        YearlyInterestRate = loan.YearlyInterestRate,
                        MonthsToPayBackLoan = loan.MonthsToPayBackLoan
                    }
                },
                Iterations = iterations
            };
            var ret = await client.SendToServer(input);
            if (ret.score.totalScore == 0)  
            {
                return -loan.YearlyInterestRate * 100;
            };
            float normalizedHappiness = ret.score.happinessScore /  12000;
            float normalizedProfit = ret.score.totalProfit / 1000000;
            float normalizedEnvironmentalImpact = ret.score.environmentalImpact / 250;

            // Assign weights
            float weightHappiness = 0.4f;
            float weightProfit = 0.8f;
            float weightEnvironmentalImpact = 0.02f;

            // Compute fitness score
            float score = (weightHappiness * normalizedHappiness) + (weightProfit * normalizedProfit) + (weightEnvironmentalImpact * normalizedEnvironmentalImpact);

            var test = -Math.Abs(ret.score.totalScore - 1337);
            if (test == 0)
            {

                var c = new ConsiditionClient();
                c.SendToServer(input);
            }
            return score; // return score for high total score
            //return test/1000000; // return to get exactly 1337
        }

    }
}
