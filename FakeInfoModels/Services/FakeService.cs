using FakeInfoModels;

namespace FakeInfo.Core.Services;

public class FakeService
{
    private Random _rand = new();

    public PersonCprOnly GetCprOnly()
    {
        var dob = DateTime.Now.AddYears(-_rand.Next(18, 80));

        return new PersonCprOnly
        {
            Cpr = GenerateCpr("male", dob)
        };
    }

    public PersonName GetName()
    {
        return new PersonName
        {
            FirstName = "Maja",
            LastName = "Jensen",
            Gender = _rand.Next(2) == 0 ? "male" : "female"
        };
    }

    public PersonNameWithDateOfBirth GetNameWithDate()
    {
        return new PersonNameWithDateOfBirth
        {
            FirstName = "Lars",
            LastName = "Hansen",
            Gender = "male",
            DateOfBirth = DateTime.Now.AddYears(-_rand.Next(18, 80))
        };
    }

    public PersonCprName GetCprName()
    {
        var name = GetName();
        var dob = DateTime.Now.AddYears(-_rand.Next(18, 80));

        return new PersonCprName
        {
            FirstName = name.FirstName,
            LastName = name.LastName,
            Gender = name.Gender,
            Cpr = GenerateCpr(name.Gender, dob)
        };
    }

    public PersonCprNameWithDateOfBirth GetCprNameAndDate()
    {
        var name = GetName();
        var dob = DateTime.Now.AddYears(-_rand.Next(18, 80));

        return new PersonCprNameWithDateOfBirth
        {
            FirstName = name.FirstName,
            LastName = name.LastName,
            Gender = name.Gender,
            DateOfBirth = dob,
            Cpr = GenerateCpr(name.Gender, dob)
        };
    }

    public PhoneOnly GetPhone()
    {
        return new PhoneOnly
        {
            Phone = "40" + _rand.Next(100000, 999999)
        };
    }

    public Address GetAddress()
    {
        return new Address
        {
            Street = "Testvej",
            Number = _rand.Next(1, 999).ToString(),
            Floor = "1",
            Door = "th",
            PostalCode = "2300",
            Town = "København"
        };
    }

    public PersonFull GetFullPerson()
    {
        var name = GetName();
        var dob = DateTime.Now.AddYears(-_rand.Next(18, 80));

        return new PersonFull
        {
            FirstName = name.FirstName,
            LastName = name.LastName,
            Gender = name.Gender,
            DateOfBirth = dob,
            Cpr = GenerateCpr(name.Gender, dob),
            Phone = GetPhone().Phone,
            Address = GetAddress()
        };
    }

    public List<PersonFull> GetPersons(int count)
    {
        if (count < 2) count = 2;
        if (count > 100) count = 100;

        var list = new List<PersonFull>();

        for (int i = 0; i < count; i++)
        {
            list.Add(GetFullPerson());
        }

        return list;
    }

    private string GenerateCpr(string gender, DateTime dob)
    {
        string datePart = dob.ToString("ddMMyy");
        string firstThree = _rand.Next(100, 1000).ToString();

        int lastDigit = gender == "female"
            ? _rand.Next(0, 5) * 2
            : _rand.Next(0, 5) * 2 + 1;

        return datePart + firstThree + lastDigit;
    }
}