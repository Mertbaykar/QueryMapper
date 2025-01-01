using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace QueryMapper.Examples.Core
{
    public static class ServiceRegistrar
    {
        public static IServiceCollection Register(this IServiceCollection services, string connString)
        {
            services
                .AddQueryMapper<BookMapper>()
                .AddAutoMapper()
                .AddDbContext<BookContext>(builder => builder.UseSqlServer(connString))
                .AddRepos()
                ;
            return services;
        }

        public static IServiceCollection AddRepos(this IServiceCollection services)
        {
            services.AddScoped<IBookRepository, BookRepository>();
            return services;
        }

        public static IServiceCollection AddAutoMapper(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(BookAutoMapper)); // AutoMapper registration
            return services;
        }
    }
}
