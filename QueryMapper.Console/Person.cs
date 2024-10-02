
namespace QueryMapper.Console
{
    public class Person
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public double? Age { get; set; }
        public List<Animal> Animals;
        public Animal Pet;
    }

    public class PersonDTO
    {
        private PersonDTO(string firstname)
        {
            Firstname = firstname;
        }

        public PersonDTO(string firstname, string lastname)
        {
            Firstname = firstname;
            Lastname = lastname;
        }

        public static PersonDTO Create(string firstname, string lastname)
        {
            return new PersonDTO(firstname, lastname);
        }

        public string Firstname { get; private set; }
        public string Lastname { get; private set; }
        public int Age { get; set; }
        public string Fullname { get; set; }
        public List<AnimalDTO> Animals;
        public AnimalDTO Pet;
    }

    public class Animal
    {
        public string Name { get; set; }
        public int age { get; set; }
    }

    public class AnimalDTO
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
