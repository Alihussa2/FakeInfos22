namespace FakeInfoModels;

public class GenerationStats
{
    public int TotalGenerated { get; set; }
    public int MaleCount { get; set; }
    public int FemaleCount { get; set; }
    public double MalePercentage { get; set; }
    public double FemalePercentage { get; set; }
    public double AverageAge { get; set; }
    public int YoungestAge { get; set; }
    public int OldestAge { get; set; }
    public List<PostalCodeStat> TopPostalCodes { get; set; } = new();
    public List<NameStat> TopFirstNames { get; set; } = new();
    public int GeneratedToday { get; set; }
    public int GeneratedThisWeek { get; set; }
}

public class PostalCodeStat
{
    public string PostalCode { get; set; } = "";
    public string Town { get; set; } = "";
    public int Count { get; set; }
}

public class NameStat
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
}