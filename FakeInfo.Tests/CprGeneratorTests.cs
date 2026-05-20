using NUnit.Framework;
using FakeInfo.Core.Services;

namespace FakeInfo.Tests;

[TestFixture]
public class CprGeneratorTests
{
    private CprGenerator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new CprGenerator();
    }

    // Format tests
    // Sikrer CPR altid har korrekt format uanset input

    [Test]
    public void GenerateCpr_Always_Returns10CharString()
    {
        // Dansk CPR er altid præcis 10 cifre
        var result = _sut.GenerateCpr("male", new DateTime(1990, 6, 15));
        Assert.That(result, Has.Length.EqualTo(10));
    }

    [Test]
    public void GenerateCpr_Always_ReturnsDigitsOnly()
    {
        // CPR må kun indeholde cifre, ingen bindestreg eller andre tegn
        var result = _sut.GenerateCpr("female", new DateTime(1985, 3, 22));
        Assert.That(result.All(char.IsDigit), Is.True,
            $"Expected all digits but got: {result}");
    }

    // Date-part tests (Black-box boundary value analysis)
    // De første 6 cifre i CPR skal altid matche fødselsdatoen på formen ddMMyy
    // Tester BV: tidlig dato, år 2000, sen dato + repræsentative værdier fra midten

    [TestCase("1990-06-15", "150690")] // normal dato
    [TestCase("2000-01-01", "010100")] // BV: nytår år 2000
    [TestCase("1950-12-31", "311250")] // BV: sidste dag på året
    [TestCase("2010-07-04", "040710")] // nyere dato
    [TestCase("1975-09-09", "090975")] // dag og måned ens
    public void GenerateCpr_DatePart_MatchesDateOfBirth(string isoDate, string expectedPrefix)
    {
        var dob = DateTime.Parse(isoDate);
        var result = _sut.GenerateCpr("male", dob);

        Assert.That(result[..6], Is.EqualTo(expectedPrefix),
            $"First 6 chars of CPR should be '{expectedPrefix}' for date {isoDate}");
    }

    // Gender tests (Black-box equivalence partitioning)
    // EP1: female → sidst ciffer skal være lige (0,2,4,6,8)
    // EP2: male   → sidst ciffer skal være ulige (1,3,5,7,9)
    // Køres 50 gange da generatoren er tilfældig

    [Test]
    public void GenerateCpr_Female_LastDigitIsEven()
    {
        for (int i = 0; i < 50; i++)
        {
            var result = _sut.GenerateCpr("female", new DateTime(1990, 1, 1));
            int lastDigit = int.Parse(result[^1].ToString());
            Assert.That(lastDigit % 2, Is.EqualTo(0),
                $"Female CPR last digit must be even, got {lastDigit} in '{result}'");
        }
    }

    [Test]
    public void GenerateCpr_Male_LastDigitIsOdd()
    {
        for (int i = 0; i < 50; i++)
        {
            var result = _sut.GenerateCpr("male", new DateTime(1990, 1, 1));
            int lastDigit = int.Parse(result[^1].ToString());
            Assert.That(lastDigit % 2, Is.EqualTo(1),
                $"Male CPR last digit must be odd, got {lastDigit} in '{result}'");
        }
    }

    // EP3: gender skal være case-insensitiv
    // "FEMALE" og "Female" skal behandles ens som "female"

    [Test]
    public void GenerateCpr_GenderFEMALE_UpperCase_LastDigitIsEven()
    {
        for (int i = 0; i < 30; i++)
        {
            var result = _sut.GenerateCpr("FEMALE", new DateTime(1990, 1, 1));
            int lastDigit = int.Parse(result[^1].ToString());
            Assert.That(lastDigit % 2, Is.EqualTo(0),
                $"'FEMALE' should produce even last digit, got {lastDigit}");
        }
    }

    [Test]
    public void GenerateCpr_GenderFemale_MixedCase_LastDigitIsEven()
    {
        for (int i = 0; i < 30; i++)
        {
            var result = _sut.GenerateCpr("Female", new DateTime(1990, 1, 1));
            int lastDigit = int.Parse(result[^1].ToString());
            Assert.That(lastDigit % 2, Is.EqualTo(0));
        }
    }

    // EP4: ukendt gender falder i else-grenen i CprGenerator
    // White-box: koden tjekker kun om gender == "female", alt andet giver ulige ciffer

    [Test]
    public void GenerateCpr_UnknownGender_LastDigitIsOdd()
    {
        for (int i = 0; i < 30; i++)
        {
            var result = _sut.GenerateCpr("other", new DateTime(1990, 1, 1));
            int lastDigit = int.Parse(result[^1].ToString());
            Assert.That(lastDigit % 2, Is.EqualTo(1),
                $"Unknown gender should fall into male/odd branch, got {lastDigit}");
        }
    }

    // Sequential-digits test (White-box, position 6-8)
    // Vi ved fra koden at _random.Next(100, 1000) bruges, så værdien er altid mellem 100-999

    [Test]
    public void GenerateCpr_SequentialPart_IsBetween100And999()
    {
        for (int i = 0; i < 30; i++)
        {
            var result = _sut.GenerateCpr("male", new DateTime(1990, 1, 1));
            string seqPart = result.Substring(6, 3);
            int seq = int.Parse(seqPart);
            Assert.That(seq, Is.InRange(100, 999),
                $"Sequential part '{seqPart}' must be between 100-999");
        }
    }

    // Parameteriseret gender-tabel (Black-box equivalence partitioning)
    // Dækker alle varianter af gender input i én test
    // Kolonne 1: gender input  |  Kolonne 2: 0 = lige ciffer (female), 1 = ulige ciffer (male)

    [TestCase("male",   1)] // normal male
    [TestCase("MALE",   1)] // stort bogstav
    [TestCase("Male",   1)] // blandet bogstav
    [TestCase("female", 0)] // normal female
    [TestCase("FEMALE", 0)] // stort bogstav
    [TestCase("Female", 0)] // blandet bogstav
    [TestCase("other",  1)] // ukendt → behandles som male
    [TestCase("",       1)] // tom streng → behandles som male
    public void GenerateCpr_GenderVariants_LastDigitParity(string gender, int expectedParity)
    {
        // Køres 20 gange per case for statistisk sikkerhed
        for (int i = 0; i < 20; i++)
        {
            var result = _sut.GenerateCpr(gender, new DateTime(1990, 6, 15));
            int lastDigit = int.Parse(result[^1].ToString());
            Assert.That(lastDigit % 2, Is.EqualTo(expectedParity),
                $"Gender='{gender}' expected parity {expectedParity}, got digit {lastDigit}");
        }
    }

    // Stability test
    // Sikrer generatoren ikke kaster exceptions ved mange kald med varierede inputs

    [Test]
    public void GenerateCpr_CalledManyTimes_NeverThrows()
    {
        Assert.DoesNotThrow(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var dob = new DateTime(1950 + i % 60, (i % 12) + 1, (i % 28) + 1);
                _sut.GenerateCpr(i % 2 == 0 ? "male" : "female", dob);
            }
        });
    }
}