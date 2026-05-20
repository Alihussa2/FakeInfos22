using NUnit.Framework;
using FakeInfo.Core.Services;

namespace FakeInfo.Tests;

/// <summary>
/// Unit tests for NameGenerator.
///
/// Black-box equivalence partitions:
///   EP1 – FirstName is a single word (no spaces), non-empty
///   EP2 – LastName is non-empty
///   EP3 – Gender is either "male" or "female"
///
/// White-box paths identified in CleanFirstName():
///   Path A – rawName is null/whitespace → returns "Ukendt"
///   Path B – rawName contains a space   → returns only first token
///   Path C – rawName has no spaces      → returned as-is
/// </summary>
[TestFixture]
public class NameGeneratorTests
{
    private NameGenerator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new NameGenerator();
    }

    [Test]
    public void GenerateName_FirstName_IsNotEmpty()
    {
        var result = _sut.GenerateName();
        Assert.That(result.FirstName, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void GenerateName_LastName_IsNotEmpty()
    {
        var result = _sut.GenerateName();
        Assert.That(result.LastName, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    [Description("EP3 – gender must be exactly 'male' or 'female'")]
    public void GenerateName_Gender_IsValidValue()
    {
        var result = _sut.GenerateName();
        Assert.That(result.Gender, Is.EqualTo("male").Or.EqualTo("female"));
    }

    [Test]
    [Description("EP1 – FirstName must not contain spaces (CleanFirstName splits on space)")]
    public void GenerateName_FirstName_ContainsNoSpaces()
    {
        for (int i = 0; i < 30; i++)
        {
            var result = _sut.GenerateName();
            Assert.That(result.FirstName, Does.Not.Contain(" "),
                $"FirstName '{result.FirstName}' should not contain spaces");
        }
    }

    [Test]
    [Description("Both male and female genders occur across 100 generated names")]
    public void GenerateName_100Times_ProducesBothGenders()
    {
        var results = Enumerable.Range(0, 100)
            .Select(_ => _sut.GenerateName().Gender)
            .ToList();

        Assert.That(results, Contains.Item("male"),   "Expected at least one 'male' result");
        Assert.That(results, Contains.Item("female"), "Expected at least one 'female' result");
    }

    [Test]
    [Description("Generator produces varied first names across 50 calls")]
    public void GenerateName_ProducesMultipleDistinctFirstNames()
    {
        var distinct = Enumerable.Range(0, 50)
            .Select(_ => _sut.GenerateName().FirstName)
            .Distinct()
            .Count();

        Assert.That(distinct, Is.GreaterThan(1),
            "Expected more than one unique first name across 50 generations");
    }

    [Test]
    [Description("GenerateName never throws when called many times")]
    public void GenerateName_CalledManyTimes_NeverThrows()
    {
        Assert.DoesNotThrow(() =>
        {
            for (int i = 0; i < 100; i++)
                _sut.GenerateName();
        });
    }
}

/// <summary>
/// Unit tests for AddressGenerator.
///
/// Black-box equivalence partitions:
///   EP1 – Street is from the known 20-item list
///   EP2 – PostalCode is exactly 4 numeric digits
///   EP3 – HouseNumber is numeric-only OR numeric + 1 letter suffix
///   EP4 – Floor is either "st" OR a numeric string 1–99
///   EP5 – Door is one of: "th"/"mf"/"tv", a number 1–50, or letter+digits pattern
///
/// Boundary values:
///   BV1 – Number = "1"  (minimum numeric house number)
///   BV2 – Number = "999" (maximum numeric house number)
///   BV3 – Floor = "st"  (ground floor special case)
///   BV4 – Floor = "1"   (minimum numeric floor)
///   BV5 – Floor = "99"  (maximum numeric floor)
/// </summary>
[TestFixture]
public class AddressGeneratorTests
{
    private AddressGenerator _sut = null!;

    private static readonly HashSet<string> KnownStreets = new()
    {
        "Nørrebrogade", "Vesterbrogade", "Østerbrogade", "Amagerbrogade",
        "Strandvejen", "Hovedgaden", "Parkvej", "Skovvej", "Engvej",
        "Bakkevej", "Kirkegade", "Stationsvej", "Industrivej", "Lindevej",
        "Birkevej", "Egevej", "Søndergade", "Vestergade", "Østergade", "Torvegade"
    };

    [SetUp]
    public void SetUp()
    {
        _sut = new AddressGenerator();
    }

    // ── EP1: Street ───────────────────────────────────────────────────────────

    [Test]
    [Description("EP1 – street name must come from the predefined list")]
    public void GenerateAddress_Street_IsFromKnownList()
    {
        var result = _sut.GenerateAddress();
        Assert.That(KnownStreets, Contains.Item(result.Street),
            $"Street '{result.Street}' is not in the known street list");
    }

    [Test]
    [Description("EP1 repeated – all streets across 50 samples are from the list")]
    public void GenerateAddress_50Times_StreetAlwaysFromKnownList()
    {
        for (int i = 0; i < 50; i++)
        {
            var result = _sut.GenerateAddress();
            Assert.That(KnownStreets, Contains.Item(result.Street),
                $"Iteration {i}: unknown street '{result.Street}'");
        }
    }

    // ── EP2: PostalCode ───────────────────────────────────────────────────────

    [Test]
    [Description("EP2 – postal code must be exactly 4 digits")]
    public void GenerateAddress_PostalCode_IsExactly4Digits()
    {
        var result = _sut.GenerateAddress();
        Assert.That(result.PostalCode, Has.Length.EqualTo(4));
        Assert.That(result.PostalCode.All(char.IsDigit), Is.True,
            $"PostalCode '{result.PostalCode}' must be numeric");
    }

    [Test]
    [Description("EP2 repeated – postal code is always 4 digits across 50 samples")]
    public void GenerateAddress_50Times_PostalCodeAlways4Digits()
    {
        for (int i = 0; i < 50; i++)
        {
            var result = _sut.GenerateAddress();
            Assert.That(result.PostalCode, Has.Length.EqualTo(4));
            Assert.That(result.PostalCode.All(char.IsDigit), Is.True);
        }
    }

    // ── EP3: HouseNumber ──────────────────────────────────────────────────────

    [Test]
    [Description("EP3 – house number must start with a digit (1–999)")]
    public void GenerateAddress_Number_StartsWithDigit()
    {
        for (int i = 0; i < 50; i++)
        {
            var result = _sut.GenerateAddress();
            Assert.That(char.IsDigit(result.Number[0]), Is.True,
                $"House number '{result.Number}' must start with a digit");
        }
    }

    [Test]
    [Description("EP3 – house number must not be empty")]
    public void GenerateAddress_Number_IsNotEmpty()
    {
        var result = _sut.GenerateAddress();
        Assert.That(result.Number, Is.Not.Null.And.Not.Empty);
    }

    // ── EP4: Floor ────────────────────────────────────────────────────────────

    [Test]
    [Description("EP4 / BV3 – floor is 'st' or a numeric string between 1 and 99")]
    public void GenerateAddress_Floor_IsStOrNumeric1To99()
    {
        for (int i = 0; i < 100; i++)
        {
            var result = _sut.GenerateAddress();
            string floor = result.Floor;

            bool isValid = floor == "st" ||
                           (int.TryParse(floor, out int num) && num >= 1 && num <= 99);

            Assert.That(isValid, Is.True,
                $"Floor '{floor}' is not 'st' or a number between 1 and 99");
        }
    }

    // ── EP5: Door ─────────────────────────────────────────────────────────────

    [Test]
    [Description("EP5 – door is not empty and not whitespace")]
    public void GenerateAddress_Door_IsNotEmpty()
    {
        for (int i = 0; i < 30; i++)
        {
            var result = _sut.GenerateAddress();
            Assert.That(result.Door, Is.Not.Null.And.Not.Empty,
                "Door value must not be null or empty");
        }
    }

    [Test]
    [Description("EP5 – door is one of: th/mf/tv, a number, or letter-digit pattern")]
    public void GenerateAddress_Door_MatchesExpectedPattern()
    {
        var namedDoors = new HashSet<string> { "th", "mf", "tv" };

        for (int i = 0; i < 100; i++)
        {
            var result = _sut.GenerateAddress();
            string door = result.Door;

            bool isNamedDoor = namedDoors.Contains(door);
            bool isNumeric = int.TryParse(door, out int dNum) && dNum >= 1 && dNum <= 50;
            bool isLetterDigit = door.Length >= 2 && char.IsLetter(door[0]);

            Assert.That(isNamedDoor || isNumeric || isLetterDigit, Is.True,
                $"Door '{door}' does not match any expected pattern");
        }
    }

    // ── Town ──────────────────────────────────────────────────────────────────

    [Test]
    public void GenerateAddress_Town_IsNotEmpty()
    {
        var result = _sut.GenerateAddress();
        Assert.That(result.Town, Is.Not.Null.And.Not.Empty);
    }

    // ── Full object ───────────────────────────────────────────────────────────

    [Test]
    public void GenerateAddress_AllFields_ArePopulated()
    {
        var result = _sut.GenerateAddress();
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

    [Test]
    [Description("Generator produces varied street names across 50 calls")]
    public void GenerateAddress_ProducesMultipleDistinctStreets()
    {
        var distinct = Enumerable.Range(0, 50)
            .Select(_ => _sut.GenerateAddress().Street)
            .Distinct()
            .Count();

        Assert.That(distinct, Is.GreaterThan(1));
    }

    [Test]
    [Description("GenerateAddress never throws when called many times")]
    public void GenerateAddress_CalledManyTimes_NeverThrows()
    {
        Assert.DoesNotThrow(() =>
        {
            for (int i = 0; i < 100; i++)
                _sut.GenerateAddress();
        });
    }
}
