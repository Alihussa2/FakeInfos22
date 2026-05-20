using System.Text.RegularExpressions;
using FakeInfoModels;

namespace FakeInfo.Core.Services;

public class AddressSearchService
{
    private static readonly List<AddressSearchResult> AllAddresses = LoadAddresses();

    public List<AddressSearchResult> Search(string? postalCode, string? town)
    {
        var query = AllAddresses.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(postalCode))
            query = query.Where(a => a.PostalCode.StartsWith(postalCode));

        if (!string.IsNullOrWhiteSpace(town))
            query = query.Where(a => a.Town.Contains(town, StringComparison.OrdinalIgnoreCase));

        return query.Take(50).ToList();
    }

    public List<AddressSearchResult> GetAll()
    {
        return AllAddresses;
    }

    private static List<AddressSearchResult> LoadAddresses()
    {
        var result = new List<AddressSearchResult>();

        string[] possiblePaths =
        {
            Path.Combine(AppContext.BaseDirectory, "Data", "addresses.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "addresses.sql")
        };

        string? sqlPath = possiblePaths.FirstOrDefault(File.Exists);
        if (sqlPath is null) return result;

        string sql = File.ReadAllText(sqlPath);

        var matches = Regex.Matches(sql, @"\('(?<postal>\d{4})',\s*'(?<town>[^']+)'\)");

        foreach (Match match in matches)
        {
            result.Add(new AddressSearchResult
            {
                PostalCode = match.Groups["postal"].Value,
                Town = match.Groups["town"].Value
            });
        }

        return result;
    }
}