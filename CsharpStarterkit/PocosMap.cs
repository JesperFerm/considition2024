namespace CsharpStarterkit
{

    public class MapData
    {
        public string name { get; set; }
        public int budget { get; set; }
        public int gameLengthInMonths { get; set; }
        public Customer[] customers { get; set; }
    }

    public class Customer
    {
        public string name { get; set; }
        public Loan loan { get; set; }
        public string personality { get; set; }
        public float capital { get; set; }
        public float income { get; set; }
        public float monthlyExpenses { get; set; }
        public int numberOfKids { get; set; }
        public float homeMortgage { get; set; }
        public bool hasStudentLoan { get; set; }
    }

    public class Loan
    {
        public string product { get; set; }
        public float environmentalImpact { get; set; }
        public float amount { get; set; }
    }
    public class InterestToPersonalityConnection
    {
        public string personalityName {  get; set; }
        public float happinessMultiplier { get; set; }
        public float acceptedMinInterest { get; set; }
        public float acceptedMaxInterest { get; set; }
        public int bestNrMonth { get; set; }
        public float livingStandardMultiplier { get; set; }
    }
}