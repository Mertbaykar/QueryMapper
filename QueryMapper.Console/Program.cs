using QueryMapper;
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
var mappedPerson = person.Map<PersonDTO>(mapper);
Console.WriteLine(JsonSerializer.Serialize(mappedPerson, serializerOptions));

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

var peopleList = new List<Person> { person, person2 };

//var extensionPeople = peopleList.Map<PersonDTO>(mapper).ToList();
//Console.WriteLine(JsonSerializer.Serialize(extensionPeople, serializerOptions));

IQueryable<Person> peopleQuery = peopleList.AsQueryable();

//var extensionQueryPeople = peopleQuery.Map<PersonDTO>(mapper).ToList();
//Console.WriteLine(JsonSerializer.Serialize(extensionQueryPeople, serializerOptions));

//var value = mapper.Map<Person, PersonDTO>(peopleQuery).ToList();
//Console.WriteLine(JsonSerializer.Serialize(value, serializerOptions));

//var value2 = mapper.Map<Person, PersonDTO>(peopleQuery).ToList();
//Console.WriteLine(JsonSerializer.Serialize(value2, serializerOptions));

//var value3 = mapper.Map<Person, PersonDTO>(peopleList);
//Console.WriteLine(JsonSerializer.Serialize(value3, serializerOptions));

//var value4 = mapper.Map<Person, PersonDTO>(person);
//Console.WriteLine(JsonSerializer.Serialize(value4, serializerOptions));
