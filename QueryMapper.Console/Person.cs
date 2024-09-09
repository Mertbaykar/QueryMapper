
namespace QueryMapper.Console
{
    public class Person
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public double? Age { get; set; }
        //public List<Person> Friends { get; set; }
        public List<Animal> Animals { get; set; }
    }

    public class PersonDTO
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public int Age { get; set; }
        //public List<PersonDTO> Friends { get; set; }
        public string Fullname { get; set; }
        public List<AnimalDTO> Animals { get; set; }
    }

    public class Animal
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public class AnimalDTO
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
