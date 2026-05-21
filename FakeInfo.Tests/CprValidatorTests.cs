using NUnit.Framework;
using FakeInfo.Core.Services;

namespace FakeInfo.Tests;

[TestFixture]
public class CprValidatorTests
{
    private CprValidator _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new CprValidator();

    // Gyldige CPR-numre skal give IsValid = true

    [TestCase("0101901234")] // normal mand
    [TestCase("1506852341")] // anden dato, mand
    [TestCase("2203752468")] // kvinde (lige sidst ciffer)
    public void ValidCpr_IsValid(string cpr)
    {
        Assert.That(_sut.Validate(cpr).IsValid, Is.True);
    }

    [Test]
    public void ValidCpr_HasNoErrors()
    {
        Assert.That(_sut.Validate("0101901234").Errors, Is.Empty);
    }

    [Test]
    public void ValidCpr_AllFlagsAreTrue()
    {
        var result = _sut.Validate("0101901234");
        Assert.Multiple(() =>
        {
            Assert.That(result.HasValidLength, Is.True);
            Assert.That(result.IsAllDigits, Is.True);
            Assert.That(result.HasValidDate, Is.True);
            Assert.That(result.HasValidGenderDigit, Is.True);
        });
    }

    // Køn detekteres korrekt fra sidst ciffer

    [Test]
    public void ValidCpr_OddLastDigit_DetectedAsMale()
    {
        Assert.That(_sut.Validate("0101901231").DetectedGender, Is.EqualTo("male"));
    }

    [Test]
    public void ValidCpr_EvenLastDigit_DetectedAsFemale()
    {
        Assert.That(_sut.Validate("0101901234").DetectedGender, Is.EqualTo("female"));
    }

    // Dato parses korrekt

    [Test]
    public void ValidCpr_DateOfBirth_ParsedCorrectly()
    {
        var result = _sut.Validate("0101901234"); // 01-01-1990
        Assert.That(result.ParsedDateOfBirth!.Value.Day, Is.EqualTo(1));
        Assert.That(result.ParsedDateOfBirth.Value.Month, Is.EqualTo(1));
    }

    [Test]
    public void ValidCpr_CalculatedAge_IsRealistic()
    {
        var result = _sut.Validate("0101901234");
        Assert.That(result.CalculatedAge, Is.InRange(0, 150));
    }

    // Tom eller null CPR skal give IsValid = false (EP2)

    [TestCase(null)]   // null
    [TestCase("")]     // tomt
    [TestCase("   ")] // whitespace
    public void EmptyOrNullCpr_IsInvalid(string? cpr)
    {
        Assert.That(_sut.Validate(cpr).IsValid, Is.False);
    }

    [Test]
    public void EmptyCpr_HasErrors()
    {
        Assert.That(_sut.Validate("").Errors, Is.Not.Empty);
    }

    // Forkert længde skal give IsValid = false (EP3, BV1-BV3)

    [TestCase("010190123")]    // 9 cifre – én under minimum
    [TestCase("01019012345")]  // 11 cifre – én over minimum
    [TestCase("1")]            // alt for kort
    public void WrongLengthCpr_IsInvalid(string cpr)
    {
        Assert.That(_sut.Validate(cpr).IsValid, Is.False);
    }

    [Test]
    public void WrongLengthCpr_HasLengthFlagFalse()
    {
        Assert.That(_sut.Validate("123456789").HasValidLength, Is.False);
    }

    // Ikke-numeriske tegn skal give IsValid = false (EP4)

    [TestCase("010190123A")] // bogstav
    [TestCase("0101-12345")] // bindestreg
    [TestCase("0101 12345")] // mellemrum
    [TestCase("010190123!")] // symbol
    public void NonNumericCpr_IsInvalid(string cpr)
    {
        Assert.That(_sut.Validate(cpr).IsValid, Is.False);
    }

    // Ugyldig dato skal give IsValid = false (EP5)

    [TestCase("3213901234")] // dag 32
    [TestCase("0013901234")] // måned 0
    [TestCase("0113901234")] // måned 13
    [TestCase("3102901234")] // 31. februar
    public void InvalidDateCpr_IsInvalid(string cpr)
    {
        Assert.That(_sut.Validate(cpr).IsValid, Is.False);
    }

    [Test]
    public void InvalidDateCpr_HasDateFlagFalse()
    {
        Assert.That(_sut.Validate("3213901234").HasValidDate, Is.False);
    }

    // Stabilitet

    [Test]
    public void Validate_ManyInputs_NeverThrows()
    {
        var inputs = new string?[] { "0101901234", "", null, "abc", "3213901234", "01019012345" };
        Assert.DoesNotThrow(() => { foreach (var c in inputs) _sut.Validate(c); });
    }
}