using FakeInfoModels;

namespace FakeInfo.Core.Services;

public class CprValidator
{
    public CprValidationResult Validate(string? cpr)
    {
        var result = new CprValidationResult { Cpr = cpr ?? "" };

        if (string.IsNullOrWhiteSpace(cpr))
        {
            result.Errors.Add("CPR-nummer er tomt");
            return result;
        }

        result.HasValidLength = cpr.Length == 10;
        if (!result.HasValidLength)
            result.Errors.Add($"CPR skal være 10 cifre, fik {cpr.Length}");

        result.IsAllDigits = cpr.All(char.IsDigit);
        if (!result.IsAllDigits)
            result.Errors.Add("CPR må kun indeholde cifre");

        if (!result.HasValidLength || !result.IsAllDigits)
        {
            result.IsValid = false;
            return result;
        }

        // Dato-tjek (ddMMyy)
        int day = int.Parse(cpr[..2]);
        int month = int.Parse(cpr[2..4]);
        int yearShort = int.Parse(cpr[4..6]);

        int centuryDigit = int.Parse(cpr[6].ToString());
        int fullYear = DetermineFullYear(yearShort, centuryDigit);

        try
        {
            var dob = new DateTime(fullYear, month, day);
            result.HasValidDate = true;
            result.ParsedDateOfBirth = dob;

            var today = DateTime.Today;
            int age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            result.CalculatedAge = age;

            if (age < 0 || age > 150)
            {
                result.Errors.Add($"Beregnet alder {age} er urealistisk");
                result.HasValidDate = false;
            }
        }
        catch
        {
            result.HasValidDate = false;
            result.Errors.Add($"Ugyldig dato: dag={day}, måned={month}, år={fullYear}");
        }

        int lastDigit = int.Parse(cpr[9].ToString());
        result.HasValidGenderDigit = true;
        result.DetectedGender = lastDigit % 2 == 0 ? "female" : "male";

        result.IsValid = result.HasValidLength
                         && result.IsAllDigits
                         && result.HasValidDate
                         && result.HasValidGenderDigit
                         && result.Errors.Count == 0;

        return result;
    }

    private static int DetermineFullYear(int yearShort, int centuryDigit)
    {
        if (centuryDigit >= 0 && centuryDigit <= 3)
            return 1900 + yearShort;
        if (centuryDigit == 4 || centuryDigit == 9)
        {
            if (yearShort <= 36) return 2000 + yearShort;
            return 1900 + yearShort;
        }
        if (yearShort <= 57) return 2000 + yearShort;
        return 1800 + yearShort;
    }
}
