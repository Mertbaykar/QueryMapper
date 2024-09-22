
namespace QueryMapper
{
    public interface IQueryMapper
    {
        public IQueryable<TDestination> Map<TSource, TDestination>(IQueryable<TSource> sourceQuery) where TSource : class where TDestination : class;
        public IEnumerable<TDestination> Map<TSource, TDestination>(IEnumerable<TSource> source) where TSource : class where TDestination : class;
        public TDestination Map<TSource, TDestination>(TSource source) where TSource : class where TDestination : class;
    }
}
