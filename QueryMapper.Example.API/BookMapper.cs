﻿
namespace QueryMapper.Examples.Core
{
    public class BookMapper : QueryMapper
    {
      
        protected override void Configure(ConfigurationBuilder builder)
        {
            builder.Configure<Person, PersonDTO>(config =>
            {
                config
                .Match(x => x.Firstname + " " + x.Lastname, dto => dto.Fullname)
                //.UsingPublicConstructor(x => new PersonDTO(x.Firstname, x.Lastname))
                ;
            });

            builder.Configure<Book, ReadBookResponse>(config =>
            {
                config
                .Match(x => x.Author.FirstName + " " + x.Author.LastName, y => y.AuthorName)
                .Match(x => x.CreatedBy.FirstName + " " + x.CreatedBy.LastName, y => y.CreatedByName)
                //.UsingNonPublicConstructor(x => new ParameterContainer(x.AuthorId))
                ;
            });

            builder.Configure<Note, ReadNoteResponse>(config =>
            {
                config
                .Match(x => x.User.FirstName + " " + x.User.LastName, y => y.UserName)
                .Match(x => x.User.ShareId, y => y.ShareId)
                ;
            });
        }
    }
}
