namespace FakeInfoModels;

public class CprValidationResult
{
    public string Cpr { get; set; } = "";
    public bool IsValid { get; set; }
    public bool HasValidLength { get; set; }
    public bool IsAllDigits { get; set; }
    public bool HasValidDate { get; set; }
    public bool HasValidGenderDigit { get; set; }
    public string DetectedGender { get; set; } = "";
    public DateTime? ParsedDateOfBirth { get; set; }
    public int? CalculatedAge { get; set; }
    public List<string> Errors { get; set; } = new();
}