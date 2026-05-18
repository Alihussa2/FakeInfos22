using NUnit.Framework;
using FakeInfoModels;
using FakeInfo.Core.Services;

namespace FakeInfo.Tests;

/// <summary>
/// Unit tests for PersonGenerator.
///
/// PersonGenerator orchestrates the sub-generators.
/// These tests verify:
///   – Each Generate* method returns a fully populated object
///   – CPR date-part matches the DateOfBirth where both are returned
///   – CPR last digit matches gender (even = female, odd = male)
///   – GenerateBulk boundary values: count = 2 (min) and count = 100 (max)
///   – DateOfBirth is within the expected range (1950–2010)
/// </summary>
[TestFixture]
public class PersonGeneratorTests
{
    private PersonGenerator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new PersonGenerator();
    }

    // ── GenerateNameOnly ─────────────────────────────────────────────────────

    [Test]
    public void GenerateNameOnly_FirstName_IsNotEmpty()
    {
        var result = _sut.GenerateNameOnly();
        Assert.That(result.FirstName, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void GenerateNameOnly_LastName_IsNotEmpty()
    {
        var result = _sut.GenerateNameOnly();
        Assert.That(result.LastName, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void GenerateNameOnly_Gender_IsValidValue()
    {
        var result = _sut.GenerateNameOnly();
        Assert.That(result.Gender, Is.EqualTo("male").Or.EqualTo("female"));
    }

    // ── GenerateNameAndDateOfBirth ────────────────────────────────────────────

    [Test]
    public void GenerateNameAndDateOfBirth_DateOfBirth_IsBetween1950And2010()
    {
        var result = _sut.GenerateNameAndDateOfBirth();
        Assert.That(result.DateOfBirth, Is.GreaterThanOrEqualTo(new DateTime(1950, 1, 1)));
        Assert.That(result.DateOfBirth, Is.LessThanOrEqualTo(new DateTime(2010, 12, 31)));
    }

    [Test]
    public void GenerateNameAndDateOfBirth_DateOfBirth_IsInThePast()
    {
        var result = _sut.GenerateNameAndDateOfBirth();
        Assert.That(result.DateOfBirth, Is.LessThan(DateTime.Now));
    }

    [Test]
    public void GenerateNameAndDateOfBirth_AllFields_ArePopulated()
    {
        var result = _sut.GenerateNameAndDateOfBirth();
        Assert.Multiple(() =>
        {
            Assert.That(result.FirstName, Is.Not.Empty);
            Assert.That(result.LastName, Is.Not.Empty);
            Assert.That(result.Gender, Is.EqualTo("male").Or.EqualTo("female"));
            Assert.That(result.DateOfBirth, Is.Not.EqualTo(default(DateTime)));
        });
    }

    // ── GenerateCprOnly ───────────────────────────────────────────────────────

    [Test]
    public void GenerateCprOnly_Cpr_IsExactly10Digits()
    {
        var result = _sut.GenerateCprOnly();
        Assert.That(result.Cpr, Has.Length.EqualTo(10));
        Assert.That(result.Cpr.All(char.IsDigit), Is.True);
    }

    [Test]
    public void GenerateCprOnly_CalledMultipleTimes_ProducesVariedResults()
    {
        var results = Enumerable.Range(0, 20)
            .Select(_ => _sut.GenerateCprOnly().Cpr)
            .Distinct()
            .Count();
        Assert.That(results, Is.GreaterThan(1));
    }

    // ── GenerateCprAndName ────────────────────────────────────────────────────

    [Test]
    public void GenerateCprAndName_Cpr_IsExactly10Digits()
    {
        var result = _sut.GenerateCprAndName();
        Assert.That(result.Cpr, Has.Length.EqualTo(10));
        Assert.That(result.Cpr.All(char.IsDigit), Is.True);
    }

    [Test]
    public void GenerateCprAndName_AllFields_ArePopulated()
    {
        var result = _sut.GenerateCprAndName();
        Assert.Multiple(() =>
        {
            Assert.That(result.Cpr, Is.Not.Empty);
            Assert.That(result.FirstName, Is.Not.Empty);
            Assert.That(result.LastName, Is.Not.Empty);
            Assert.That(result.Gender, Is.EqualTo("male").Or.EqualTo("female"));
        });
    }

    // ── GenerateCprNameAndDateOfBirth ─────────────────────────────────────────

    [Test]
    public void GenerateCprNameAndDateOfBirth_CprDatePart_MatchesDateOfBirth()
    {
        // Run multiple times to rule out accidental match
        for (int i = 0; i < 20; i++)
        {
            var result = _sut.GenerateCprNameAndDateOfBirth();
            string expectedDatePart = result.DateOfBirth.ToString("ddMMyy");
            Assert.That(result.Cpr[..6], Is.EqualTo(expectedDatePart),
                $"CPR '{result.Cpr}' date part does not match DateOfBirth {result.DateOfBirth:dd-MM-yyyy}");
        }
    }

    [Test]
    public void GenerateCprNameAndDateOfBirth_CprGenderDigit_MatchesGender()
    {
        for (int i = 0; i < 30; i++)
        {
            var result = _sut.GenerateCprNameAndDateOfBirth();
            int lastDigit = int.Parse(result.Cpr[^1].ToString());

            if (result.Gender == "female")
                Assert.That(lastDigit % 2, Is.EqualTo(0),
                    $"Female CPR '{result.Cpr}' must end with even digit");
            else
                Assert.That(lastDigit % 2, Is.EqualTo(1),
                    $"Male CPR '{result.Cpr}' must end with odd digit");
        }
    }

    [Test]
    public void GenerateCprNameAndDateOfBirth_DateOfBirth_WithinRange()
    {
        for (int i = 0; i < 20; i++)
        {
            var result = _sut.GenerateCprNameAndDateOfBirth();
            Assert.That(result.DateOfBirth, Is.GreaterThanOrEqualTo(new DateTime(1950, 1, 1)));
            Assert.That(result.DateOfBirth, Is.LessThanOrEqualTo(new DateTime(2010, 12, 31)));
        }
    }

    // ── GenerateAddressOnly ───────────────────────────────────────────────────

    [Test]
    public void GenerateAddressOnly_Street_IsFromKnownList()
    {
        var knownStreets = new[]
        {
            "Nørrebrogade", "Vesterbrogade", "Østerbrogade", "Amagerbrogade",
            "Strandvejen", "Hovedgaden", "Parkvej", "Skovvej", "Engvej",
            "Bakkevej", "Kirkegade", "Stationsvej", "Industrivej", "Lindevej",
            "Birkevej", "Egevej", "Søndergade", "Vestergade", "Østergade", "Torvegade"
        };
        var result = _sut.GenerateAddressOnly();
        Assert.That(result.Street, Is.AnyOf(knownStreets));
    }

    [Test]
    public void GenerateAddressOnly_PostalCode_Is4Digits()
    {
        var result = _sut.GenerateAddressOnly();
        Assert.That(result.PostalCode, Has.Length.EqualTo(4));
        Assert.That(result.PostalCode.All(char.IsDigit), Is.True);
    }

    [Test]
    public void GenerateAddressOnly_AllFields_AreNonEmpty()
    {
        var result = _sut.GenerateAddressOnly();
        Assert.Multiple(() =>
        {
            Assert.That(result.Street, Is.Not.Empty);
            Assert.That(result.Number, Is.Not.Empty);
            Assert.That(result.Floor, Is.Not.Empty);
            Assert.That(result.Door, Is.Not.Empty);
            Assert.That(result.PostalCode, Is.Not.Empty);
            Assert.That(result.Town, Is.Not.Empty);
        });
    }

    // ── GeneratePhoneOnly ─────────────────────────────────────────────────────

    [Test]
    public void GeneratePhoneOnly_Phone_IsExactly8Digits()
    {
        var result = _sut.GeneratePhoneOnly();
        Assert.That(result.Phone, Has.Length.EqualTo(8));
        Assert.That(result.Phone.All(char.IsDigit), Is.True);
    }

    // ── GenerateFullPerson ────────────────────────────────────────────────────

    [Test]
    public void GenerateFullPerson_AllTopLevelFields_ArePopulated()
    {
        var result = _sut.GenerateFullPerson();
        Assert.Multiple(() =>
        {
            Assert.That(result.Cpr, Is.Not.Empty);
            Assert.That(result.FirstName, Is.Not.Empty);
            Assert.That(result.LastName, Is.Not.Empty);
            Assert.That(result.Gender, Is.EqualTo("male").Or.EqualTo("female"));
            Assert.That(result.Phone, Is.Not.Empty);
            Assert.That(result.Address, Is.Not.Null);
        });
    }

    [Test]
    public void GenerateFullPerson_Cpr_IsExactly10Digits()
    {
        var result = _sut.GenerateFullPerson();
        Assert.That(result.Cpr, Has.Length.EqualTo(10));
        Assert.That(result.Cpr.All(char.IsDigit), Is.True);
    }

    [Test]
    public void GenerateFullPerson_CprDatePart_MatchesDateOfBirth()
    {
        for (int i = 0; i < 20; i++)
        {
            var result = _sut.GenerateFullPerson();
            string expected = result.DateOfBirth.ToString("ddMMyy");
            Assert.That(result.Cpr[..6], Is.EqualTo(expected),
                $"CPR date part does not match DateOfBirth for: {result.FirstName} {result.LastName}");
        }
    }

    [Test]
    public void GenerateFullPerson_CprGenderDigit_MatchesGender()
    {
        for (int i = 0; i < 30; i++)
        {
            var result = _sut.GenerateFullPerson();
            int lastDigit = int.Parse(result.Cpr[^1].ToString());

            if (result.Gender == "female")
                Assert.That(lastDigit % 2, Is.EqualTo(0));
            else
                Assert.That(lastDigit % 2, Is.EqualTo(1));
        }
    }

    [Test]
    public void GenerateFullPerson_Address_IsFullyPopulated()
    {
        var result = _sut.GenerateFullPerson();
        Assert.Multiple(() =>
        {
            Assert.That(result.Address.Street, Is.Not.Empty);
            Assert.That(result.Address.Number, Is.Not.Empty);
            Assert.That(result.Address.Floor, Is.Not.Empty);
            Assert.That(result.Address.Door, Is.Not.Empty);
            Assert.That(result.Address.PostalCode, Is.Not.Empty);
            Assert.That(result.Address.Town, Is.Not.Empty);
        });
    }

    [Test]
    public void GenerateFullPerson_Phone_IsExactly8Digits()
    {
        var result = _sut.GenerateFullPerson();
        Assert.That(result.Phone, Has.Length.EqualTo(8));
        Assert.That(result.Phone.All(char.IsDigit), Is.True);
    }

    // ── GenerateBulk – boundary value tests ───────────────────────────────────

    [TestCase(2,   2,   TestName = "Bulk_BV_MinBoundary_Returns2")]
    [TestCase(3,   3,   TestName = "Bulk_JustAboveMin_Returns3")]
    [TestCase(10,  10,  TestName = "Bulk_TypicalValue_Returns10")]
    [TestCase(50,  50,  TestName = "Bulk_MidRange_Returns50")]
    [TestCase(99,  99,  TestName = "Bulk_JustBelowMax_Returns99")]
    [TestCase(100, 100, TestName = "Bulk_BV_MaxBoundary_Returns100")]
    public void GenerateBulk_ValidCount_ReturnsExactCount(int input, int expected)
    {
        var result = _sut.GenerateBulk(input);
        Assert.That(result, Has.Count.EqualTo(expected));
    }

    [Test]
    [Description("Each person in bulk list must have a fully populated Cpr (10 digits)")]
    public void GenerateBulk_EachPerson_HasValid10DigitCpr()
    {
        var results = _sut.GenerateBulk(10);
        foreach (var person in results)
        {
            Assert.That(person.Cpr, Has.Length.EqualTo(10),
                $"Person {person.FirstName} {person.LastName} has invalid CPR: '{person.Cpr}'");
            Assert.That(person.Cpr.All(char.IsDigit), Is.True);
        }
    }

    [Test]
    [Description("Each person in bulk list must have a valid gender")]
    public void GenerateBulk_EachPerson_HasValidGender()
    {
        var results = _sut.GenerateBulk(20);
        foreach (var person in results)
        {
            Assert.That(person.Gender, Is.EqualTo("male").Or.EqualTo("female"));
        }
    }

    [Test]
    [Description("Each person in bulk list must have a non-empty phone number")]
    public void GenerateBulk_EachPerson_HasNonEmptyPhone()
    {
        var results = _sut.GenerateBulk(20);
        foreach (var person in results)
        {
            Assert.That(person.Phone, Is.Not.Null.And.Not.Empty);
        }
    }

    [Test]
    [Description("Bulk generates diverse people, not duplicates of the same record")]
    public void GenerateBulk_10Persons_HasAtLeastSomeVariety()
    {
        var results = _sut.GenerateBulk(10);
        var distinctNames = results.Select(p => p.FirstName + p.LastName).Distinct().Count();
        Assert.That(distinctNames, Is.GreaterThan(1),
            "Expected diverse first/last name combinations in bulk output");
    }
}
