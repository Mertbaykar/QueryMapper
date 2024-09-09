using QueryMapper.Console;
using System.Text.Json;

var mapper = new CustomMapper();

var person = new Person
{
    Firstname = "Mert",
    Lastname = "Baykar",
    Animals =
    [
        new Animal { Name = "Mert'in Kedisi" }
    ],
    Age = 26.3
};

var person2 = new Person
{
    Firstname = "Berk",
    Lastname = "Baykar",
    Animals =
    [
        new Animal { Name = "Berk'in Kedisi" }
    ],
    Age = 26.3
};

IQueryable<Person> people = new List<Person> { person, person2 }.AsQueryable();
var value = mapper.Map<Person, PersonDTO>(people).ToList();

Console.WriteLine(JsonSerializer.Serialize(value,
    new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    }));