using AutoMapper;


namespace QueryMapper.Examples.Core
{

    public class BookAutoMapper : Profile
    {
        public BookAutoMapper()
        {
            CreateMap<Person, PersonDTO>()
                .ForMember(x => x.Fullname, dto => dto.MapFrom(y => y.Firstname + " " + y.Lastname));

            CreateMap<Book, ReadBookResponse>()
               .ForMember(x => x.AuthorName, y => y.MapFrom(z => z.Author.FirstName + " " + z.Author.LastName));

            CreateMap<Note, ReadNoteResponse>()
           .ForMember(x => x.UserName, y => y.MapFrom(z => z.User.FirstName + " " + z.User.LastName))
           .ForMember(x => x.ShareId, y => y.MapFrom(z => z.User.ShareId))
           ;
        }
    }
}
