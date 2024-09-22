
namespace QueryMapper
{

    public static class QueryMapperExtensions
    {

        public static IQueryable<TDestination> Map<TDestination>(this IQueryable<object> sourceQuery, IQueryMapper queryMapper)
            where TDestination : class
        {
            var sourceType = sourceQuery.ElementType; // IQueryable'dan TSource tipini alır
            EnsureClass(sourceType);
            // Sadece IQueryable versiyonunu bulur
            var mapMethod = typeof(IQueryMapper).GetMethods()
                            .FirstOrDefault(m => m.Name == nameof(queryMapper.Map)
                                                 && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IQueryable<>))!;
            var genericMapMethod = mapMethod.MakeGenericMethod(sourceType, typeof(TDestination));

            return (IQueryable<TDestination>)genericMapMethod.Invoke(queryMapper, [sourceQuery])!;
        }

        public static IEnumerable<TDestination> Map<TDestination>(this IEnumerable<object> source, IQueryMapper queryMapper)
           where TDestination : class
        {
            var sourceType = source.GetType().GetGenericArguments().First();
            EnsureClass(sourceType);

            var mapMethod = typeof(IQueryMapper).GetMethods()
                            .FirstOrDefault(m => m.Name == nameof(queryMapper.Map)
                                                 && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))!;
            var genericMapMethod = mapMethod.MakeGenericMethod(sourceType, typeof(TDestination));

            return (IEnumerable<TDestination>)genericMapMethod.Invoke(queryMapper, [source])!;
        }

        public static TDestination Map<TDestination>(this object source, IQueryMapper queryMapper)
          where TDestination : class
        {
            var sourceType = source.GetType();
            EnsureClass(sourceType);

            var mapMethod = typeof(IQueryMapper).GetMethods()
                             .FirstOrDefault(m => m.Name == nameof(queryMapper.Map)
                                                  && m.GetParameters()[0].ParameterType.IsClass)!;
            var genericMapMethod = mapMethod.MakeGenericMethod(sourceType, typeof(TDestination));

            return (TDestination)genericMapMethod.Invoke(queryMapper, [source])!;
        }

        private static void EnsureClass(Type type)
        {
            if (!(type.IsClass && type != typeof(string)))
                throw new ArgumentException($"{type.Name} should be class");
        }
    }
}
