namespace FakeInfo.Core.Data;

public class GeneratedPersonEntity
{
    public int Id { get; set; }

    public string Cpr { get; set; } = "";
    public DateTime DateOfBirth { get; set; }

    public string Phone { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Gender { get; set; } = "";

    public string Street { get; set; } = "";
    public string Number { get; set; } = "";
    public string Floor { get; set; } = "";
    public string Door { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Town { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}