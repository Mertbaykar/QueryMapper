
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
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public int Age { get; set; }
        public string Fullname { get; set; }
        public List<AnimalDTO> Animals;
        public AnimalDTO Pet;
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
