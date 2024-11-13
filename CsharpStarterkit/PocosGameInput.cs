using CsharpStarterkit;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsharpStarterkit;

public class GameInput
{
    public required string MapName { get; init; }
    public required List<CustomerLoanRequestProposal> Proposals { get; init; }
    public required List<Dictionary<string, CustomerAction>> Iterations { get; set; }
}
public class CustomerLoanRequestProposal
{
    public string CustomerName { get; set; } = null!;
    public float YearlyInterestRate { get; set; }
    public int MonthsToPayBackLoan { get; set; }

}

public enum CustomerActionType
{
    Skip,
    Punish,
    Award
}
public record CustomerAction
{
    public string Type { get; set; }
    public string Award { get; set; }
}

public enum AwardType
{
    None,
    IkeaCheck,
    IkeaFoodCoupon,
    IkeaDeliveryCheck,
    NoInterestRate,
    GiftCard,
    HalfInterestRate,
}




public class Award
{
    public float cost { get; set; }
    public int baseHappiness { get; set; }
    public string name { get; set; }
}

public class Personality
{
    public float happinessMultiplier { get; set; }
    public float acceptedMinInterest { get; set; }
    public float acceptedMaxInterest { get; set; }
    public float livingStandardMultiplier { get; set; }

    public Personality(float happinessMultiplier, float acceptedMinInterest, float acceptedMaxInterest, float livingStandardMultiplier)
    {
        this.happinessMultiplier = happinessMultiplier;
        this.acceptedMinInterest = acceptedMinInterest;
        this.acceptedMaxInterest = acceptedMaxInterest;
        this.livingStandardMultiplier = livingStandardMultiplier;
    }
}
public class awardCalcObj
{
    public Award award { get; set; }
    public float cost { get; set; }
}