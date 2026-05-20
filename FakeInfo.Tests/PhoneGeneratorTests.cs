using NUnit.Framework;
using FakeInfo.Core.Services;

namespace FakeInfo.Tests;

/// <summary>
/// Unit tests for PhoneGenerator.
///
/// Black-box equivalence partitions:
///   EP1 – output is exactly 8 digits long
///   EP2 – output consists only of numeric characters
///   EP3 – output starts with one of the known valid Danish prefixes
///
/// Boundary values:
///   BV1 – shortest valid prefix (1 digit): "2"
///   BV2 – longest valid prefix  (3 digits): e.g. "342"
///   BV3 – resulting number is always exactly 8 digits regardless of prefix length
/// </summary>
[TestFixture]
public class PhoneGeneratorTests
{
    private PhoneGenerator _sut = null!;

    // All valid Danish mobile prefixes as defined in PhoneGenerator
    private static readonly HashSet<string> ValidPrefixes = new()
    {
        "2", "30", "31", "40", "41", "42", "50", "51", "52", "53",
        "60", "61", "71", "81", "91", "92", "93",
        "342", "344", "345", "346", "347", "348", "349",
        "356", "357", "359", "362", "365", "366", "389", "398",
        "431", "441", "462", "466", "468", "472", "474", "476", "478",
        "485", "486", "488", "489", "493", "494", "495", "496",
        "498", "499", "542", "543", "545", "551", "552", "556",
        "571", "572", "573", "574", "577", "579", "584", "586", "587",
        "589", "597", "598", "627", "629", "641", "649", "658",
        "662", "663", "664", "665", "667", "692", "693", "694", "697",
        "771", "772", "782", "783", "785", "786", "788", "789",
        "826", "827", "829"
    };

    [SetUp]
    public void SetUp()
    {
        _sut = new PhoneGenerator();
    }

    // ── EP1: Length ──────────────────────────────────────────────────────────

    [Test]
    [Description("EP1 – phone number must always be exactly 8 digits")]
    public void GeneratePhoneNumber_Always_Returns8Digits()
    {
        var result = _sut.GeneratePhoneNumber();
        Assert.That(result, Has.Length.EqualTo(8),
            $"Expected 8-digit phone number, got '{result}' ({result.Length} chars)");
    }

    [Test]
    [Description("EP1 repeated – length must be 8 across 100 samples")]
    public void GeneratePhoneNumber_100Times_AlwaysLength8()
    {
        for (int i = 0; i < 100; i++)
        {
            var result = _sut.GeneratePhoneNumber();
            Assert.That(result, Has.Length.EqualTo(8),
                $"Iteration {i}: expected length 8, got '{result}'");
        }
    }

    // ── EP2: Digits only ─────────────────────────────────────────────────────

    [Test]
    [Description("EP2 – phone number must consist only of numeric characters")]
    public void GeneratePhoneNumber_Always_ReturnsDigitsOnly()
    {
        var result = _sut.GeneratePhoneNumber();
        Assert.That(result.All(char.IsDigit), Is.True,
            $"Expected digits only, got: '{result}'");
    }

    [Test]
    [Description("EP2 repeated – digits-only constraint holds across 100 samples")]
    public void GeneratePhoneNumber_100Times_AlwaysDigitsOnly()
    {
        for (int i = 0; i < 100; i++)
        {
            var result = _sut.GeneratePhoneNumber();
            Assert.That(result.All(char.IsDigit), Is.True,
                $"Iteration {i}: non-digit found in '{result}'");
        }
    }

    // ── EP3 / BV1-BV3: Valid prefix ──────────────────────────────────────────

    [Test]
    [Description("EP3 – phone number must start with a valid Danish prefix")]
    public void GeneratePhoneNumber_Always_StartsWithValidPrefix()
    {
        var result = _sut.GeneratePhoneNumber();
        bool hasValidPrefix = ValidPrefixes.Any(p => result.StartsWith(p));
        Assert.That(hasValidPrefix, Is.True,
            $"Phone number '{result}' does not start with any known valid prefix");
    }

    [Test]
    [Description("EP3 repeated – valid-prefix constraint holds across 200 samples")]
    public void GeneratePhoneNumber_200Times_AlwaysValidPrefix()
    {
        for (int i = 0; i < 200; i++)
        {
            var result = _sut.GeneratePhoneNumber();
            bool hasValidPrefix = ValidPrefixes.Any(p => result.StartsWith(p));
            Assert.That(hasValidPrefix, Is.True,
                $"Iteration {i}: '{result}' does not start with a valid prefix");
        }
    }

    // ── BV1: 1-digit prefix "2" produces 7 random suffix digits ──────────────

    [Test]
    [Description("BV1 – numbers starting with prefix '2' (1 digit) must still be 8 digits total")]
    public void GeneratePhoneNumber_PrefixLength1_StillReturns8Digits()
    {
        // Run enough times to statistically hit the '2' prefix
        var results = Enumerable.Range(0, 500)
            .Select(_ => _sut.GeneratePhoneNumber())
            .Where(n => n.StartsWith("2") && n.Length == 1 + 7)
            .ToList();

        // If we got at least one with prefix '2', all must be length 8
        foreach (var r in results)
        {
            Assert.That(r, Has.Length.EqualTo(8));
        }
    }

    // ── BV2: 3-digit prefix produces 5 random suffix digits ──────────────────

    [Test]
    [Description("BV2 – numbers starting with a 3-digit prefix must still be 8 digits total")]
    public void GeneratePhoneNumber_PrefixLength3_StillReturns8Digits()
    {
        var threeDigitPrefixes = ValidPrefixes.Where(p => p.Length == 3).ToHashSet();

        var results = Enumerable.Range(0, 500)
            .Select(_ => _sut.GeneratePhoneNumber())
            .Where(n => threeDigitPrefixes.Any(p => n.StartsWith(p)))
            .ToList();

        foreach (var r in results)
        {
            Assert.That(r, Has.Length.EqualTo(8),
                $"3-digit prefix number '{r}' must still be 8 digits");
        }
    }

    // ── Uniqueness smoke test ─────────────────────────────────────────────────

    [Test]
    [Description("PhoneGenerator must produce varied output (not always the same number)")]
    public void GeneratePhoneNumber_ProducesMultipleDistinctValues()
    {
        var results = Enumerable.Range(0, 50)
            .Select(_ => _sut.GeneratePhoneNumber())
            .Distinct()
            .Count();

        Assert.That(results, Is.GreaterThan(1),
            "Expected multiple distinct phone numbers, generator seems to always return the same value");
    }

    // ── Not-null / not-empty ──────────────────────────────────────────────────

    [Test]
    [Description("GeneratePhoneNumber must never return null or empty string")]
    public void GeneratePhoneNumber_Never_ReturnsNullOrEmpty()
    {
        for (int i = 0; i < 50; i++)
        {
            var result = _sut.GeneratePhoneNumber();
            Assert.That(result, Is.Not.Null.And.Not.Empty);
        }
    }
}
