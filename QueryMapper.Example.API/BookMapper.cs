
namespace QueryMapper.Examples.Core
{
    public class BookMapper : QueryMapper
    {
        public BookMapper()
        {
            Configure<Person, PersonDTO>(config =>
            {
                config
                .Match(x => x.Firstname + " " + x.Lastname, dto => dto.Fullname)
                .UsingConstructor(x => new PersonDTO(x.Firstname, x.Lastname))
                ;
            });

            Configure<Book, ReadBookResponse>(config =>
            {
                config
                .Match(x => x.Author.FirstName + " " + x.Author.LastName, y => y.AuthorName)
                .Match(x => x.CreatedBy.FirstName + " " + x.CreatedBy.LastName, y => y.CreatedByName)
                .UsingConstructor(x => x.AddParameter(book => book.AuthorId)
                );
            });

            Configure<Note, ReadNoteResponse>(config =>
            {
                config
                .Match(x => x.User.FirstName + " " + x.User.LastName, y => y.UserName)
                .Match(x => x.User.ShareId, y => y.ShareId)
                ;
            });
        }
    }
}
