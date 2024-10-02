using System.ComponentModel.DataAnnotations.Schema;

namespace QueryMapper.Examples.Core
{
    public class Author : EntityBase
    {
        private Author()
        {
            
        }

        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        [NotMapped]
        public string FullName => FirstName + " " + LastName;

        public ICollection<Book> Books { get; private set; } = new List<Book>();

        //public static Author Create(CreateAuthorRequest createAuthorRequest)
        //{
        //    Author author = new Author();
        //    author.FirstName = createAuthorRequest.FirstName;
        //    author.LastName = createAuthorRequest.LastName;
        //    return author;
        //}
    }
}
