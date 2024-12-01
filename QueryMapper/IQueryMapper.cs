
using System.Linq.Expressions;

namespace QueryMapper
{
    public interface IQueryMapper
    {
        IQueryable<TDestination> Map<TSource, TDestination>(IQueryable<TSource> sourceQuery) where TSource : class where TDestination : class;
        IEnumerable<TDestination> Map<TSource, TDestination>(IEnumerable<TSource> source) where TSource : class where TDestination : class;
        TDestination Map<TSource, TDestination>(TSource source) where TSource : class where TDestination : class;
        /// <summary>
        /// Lets you inspect mapping expression from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/>
        /// </summary>
        /// <typeparam name="TSource">Source type. Could be IEnumerable, IQueryable or class</typeparam>
        /// <typeparam name="TDestination">Destination type. Could be IEnumerable, IQueryable or any class</typeparam>
        /// <returns>Mapping expression</returns>
        string GetMappingExpression<TSource, TDestination>() where TSource : class where TDestination : class;
    }
}
