using System.ComponentModel.DataAnnotations.Schema;

namespace QueryMapper.Console
{
    public class Book : EntityBase
    {
        private Book()
        {
            
        }

        public string Name { get; private set; }
        public string Summary { get; private set; }
        public int PublishYear { get; private set; }
        
        public string ShelfLocation { get; private set; }
        public DateTime CreatedDate { get; private set; }
        
        public string CoverImagePath { get; private set; }

        [ForeignKey(nameof(AuthorId))]
        public Author Author { get; private set; }
        public int AuthorId { get; private set; }

        [ForeignKey(nameof(CreatedById))]
        public User CreatedBy { get; private set; }
        public int CreatedById { get; private set; }

        public ICollection<Note> Notes { get; private set; } = new List<Note>();


        //public static Book Create(CreateBookRequest createBookRequest)
        //{
        //    Book book = new Book();
        //    book.Name = createBookRequest.Name;
        //    book.Summary = createBookRequest.Summary;
        //    book.ShelfLocation = createBookRequest.ShelfLocation;
        //    book.PublishYear = createBookRequest.PublishYear;
        //    book.CoverImagePath = createBookRequest.CoverImagePath;
        //    book.CreatedById = createBookRequest.CreatedById;
        //    book.AuthorId = createBookRequest.AuthorId;
        //    book.CreatedDate = DateTime.Now;
        //    return book;
        //}

        //public void ChangeCoverImage(string path)
        //{
        //    CoverImagePath = path;
        //}

        //public void Update(string name, string summary, int publishYear, string shelfLocation)
        //{
        //    Name = name;
        //    Summary = summary;
        //    PublishYear = publishYear;
        //    ShelfLocation = shelfLocation;
        //}
    }
}
