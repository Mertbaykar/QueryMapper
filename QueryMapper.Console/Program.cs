using QueryMapper.Console;
using System.Text.Json;

var serializerOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    IncludeFields = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
};

var mapper = new CustomMapper();

var person = new Person
{
    Firstname = "Mert",
    Lastname = "Baykar",
    //Animals =
    //[
    //    new Animal { Name = "Mert'in Kedisi" }
    //],
    Age = 26.3,
    Pet = new Animal
    {
        Name = "Rengar",
        Age = 23
    }
};

var person2 = new Person
{
    Firstname = "Berk",
    Lastname = "Baykar",
    Animals =
    [
        new Animal { Name = "Pamuk" },null
    ],
    Age = 26.3
};

IQueryable<Person> people = new List<Person> { person, person2 }.AsQueryable();
var value = mapper.Map<Person, PersonDTO>(people).ToList();
var value2 = mapper.Map<Person, PersonDTO>(people).ToList();


Console.WriteLine(JsonSerializer.Serialize(value, serializerOptions));
Console.WriteLine(JsonSerializer.Serialize(value2, serializerOptions));
