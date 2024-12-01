using Microsoft.Extensions.DependencyInjection;

namespace QueryMapper
{
    public static class QueryMapperRegistrar
    {

        public static IServiceCollection AddQueryMapper<TQueryMapper>(this IServiceCollection services) where TQueryMapper : QueryMapper, new()
        {
            return services.AddSingleton<IQueryMapper>(new TQueryMapper());
        }
    }
}
