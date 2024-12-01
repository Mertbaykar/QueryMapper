
namespace QueryMapper.Examples.Core
{
    public class BookRepository : IBookRepository
    {

        private readonly BookContext BookContext;
        private readonly IQueryMapper QueryMapper;

        public BookRepository(BookContext bookContext, IQueryMapper queryMapper)
        {
            BookContext = bookContext;
            QueryMapper = queryMapper;
        }

        public List<ReadBookResponse> Get()
        {
            var query = BookContext.Book.Map<ReadBookResponse>(QueryMapper);
            return query.ToList();
        }

        public string GetExpression() =>
             QueryMapper.GetMappingExpression<Book, ReadBookResponse>();

    }

    public interface IBookRepository
    {
        List<ReadBookResponse> Get();
        string GetExpression();
    }
}
