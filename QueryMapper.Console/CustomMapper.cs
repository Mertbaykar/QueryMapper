
namespace QueryMapper.Console
{
    public class CustomMapper : QueryMapper
    {
        public CustomMapper()
        {
            Configure<Person, PersonDTO>(config =>
            {
                config.Match(entity => entity.Firstname + " " + entity.Lastname, dto => dto.Fullname);
            });
        }
    }
}
