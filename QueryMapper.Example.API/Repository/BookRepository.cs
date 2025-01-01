
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace QueryMapper.Examples.Core
{
    public class BookRepository : IBookRepository
    {

        private readonly BookContext BookContext;
        private readonly IQueryMapper QueryMapper;
        private readonly IMapper AutoMapper;

        public BookRepository(BookContext bookContext, IQueryMapper queryMapper, IMapper autoMapper)
        {
            BookContext = bookContext;
            QueryMapper = queryMapper;
            AutoMapper = autoMapper;
        }

        public List<ReadBookResponse> Get()
        {
            var query = BookContext.Book.Map<ReadBookResponse>(QueryMapper);
            return query.ToList();
        }

        public List<ReadBookResponse> GetByAutoMapper()
        {
            
            var query = BookContext.Book.ProjectTo<ReadBookResponse>(AutoMapper.ConfigurationProvider);
            return query.ToList();
        }

        public string GetExpression() =>
             QueryMapper.GetMappingExpression<Book, ReadBookResponse>();

    }

    public interface IBookRepository
    {
        List<ReadBookResponse> Get();
        List<ReadBookResponse> GetByAutoMapper();
        string GetExpression();
    }
}
