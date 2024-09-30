
namespace QueryMapper.Console
{
    public class CustomMapper : QueryMapper
    {
        public CustomMapper()
        {
            Configure<Person, PersonDTO>(config =>
            {
                config
                .Match(x => x.Firstname + " " + x.Lastname, dto => dto.Fullname)
                .UsingConstructor(x => PersonDTO.Create(x.Firstname, x.Lastname))
                //.UsingConstructor(x => new PersonDTO(x.Firstname, x.Lastname))
                ;
            });
        }
    }
}
