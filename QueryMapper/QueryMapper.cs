
namespace QueryMapper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public abstract class QueryMapper : IQueryMapper
    {

        private readonly HashSet<MapperConfiguration> _configurations = new();

        public QueryMapper Configure<TSource, TDestination>(Action<MapperConfiguration<TSource, TDestination>>? configuration = null)
            where TSource : class
            where TDestination : class
        {
            if (_configurations.Any(c => c.SourceType == typeof(TSource) && c.DestinationType == typeof(TDestination)))
                throw new Exception($"Configuration for mapping from {typeof(TSource).Name} type to {typeof(TDestination).Name} is defined more than once. Check mapper code");

            var config = new MapperConfiguration(typeof(TSource), typeof(TDestination));

            using (var configTemp = new MapperConfiguration<TSource, TDestination>())
            {
                configuration?.Invoke(configTemp);
                configTemp.Matchings.ForEach(config.Matchings.Add);
                if (configTemp.CtorExpression != null)
                    config.SetCtorExpression(configTemp.CtorExpression);
                _configurations.Add(config);
            }

            return this;
        }

        public IQueryable<TDestination> Map<TSource, TDestination>(IQueryable<TSource> sourceQuery) where TSource : class where TDestination : class
        {
            var selector = CreateMapExpression<TSource, TDestination>();
            return sourceQuery.Select(selector);
        }

        public IEnumerable<TDestination> Map<TSource, TDestination>(IEnumerable<TSource> source) where TSource : class where TDestination : class
        {
            var selector = CreateMapExpression<TSource, TDestination>().Compile();
            return source.Select(selector);
        }

        public TDestination Map<TSource, TDestination>(TSource source) where TSource : class where TDestination : class
        {
            var selector = CreateMapExpression<TSource, TDestination>().Compile();
            return selector(source);
        }

        private Expression<Func<TSource, TDestination>> CreateMapExpression<TSource, TDestination>()
        {
            var sourceParameter = Expression.Parameter(typeof(TSource), typeof(TSource).Name);
            var body = CreateMapExpressionCore(typeof(TSource), typeof(TDestination), sourceParameter);
            return Expression.Lambda<Func<TSource, TDestination>>(body, sourceParameter);
        }

        private LambdaExpression CreateMapExpression(Type sourceType, Type destType)
        {
            var sourceParameter = Expression.Parameter(sourceType, sourceType.Name);
            var body = CreateMapExpressionCore(sourceType, destType, sourceParameter);
            var lambdaType = typeof(Func<,>).MakeGenericType(sourceType, destType);
            return Expression.Lambda(lambdaType, body, sourceParameter);
        }

        private MemberInitExpression CreateMapExpressionCore(Type sourceType, Type destType, ParameterExpression sourceParameter)
        {
            MapperConfiguration? config = this._configurations.FirstOrDefault(x => x.SourceType == sourceType && x.DestinationType == destType);

            if (config?.MemberInitExpression != null)
                return config.MemberInitExpression;

            var bindings =
                 GetMembersWriteable(destType)
                .Select(destMember => CreateBinding(destMember, sourceParameter, sourceType))
                .Where(binding => binding != null);

            var ctorExp = CreateConstructorExpression(sourceType, destType, sourceParameter);
            MemberInitExpression memberInitExpression = Expression.MemberInit(ctorExp, bindings);

            if (config == null)
                config = this._configurations.First(x => x.SourceType == sourceType && x.DestinationType == destType);

            config.SetMemberInitExpression(memberInitExpression);
            return memberInitExpression;
        }

        private NewExpression CreateConstructorExpression(Type sourceType, Type destType, ParameterExpression sourceParameter)
        {
            MapperConfiguration? config = this._configurations.FirstOrDefault(x => x.SourceType == sourceType && x.DestinationType == destType);
            if (config?.CtorExpression != null)
            {
                var newArguments = config.CtorExpression.Arguments
                    .Select(arg => new ParameterReplacer(ExtractParameterExpression(arg)!, sourceParameter).Visit(arg));
                return Expression.New(config.CtorExpression.Constructor!, newArguments);
            }

            ConstructorInfo? constructor;
            constructor = destType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
            if (constructor == null)
                constructor = destType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();

            Expression sourceExpr;
            Expression? ctorArgExp;

            List<Expression> arguments = new();

            foreach (var parameter in constructor!.GetParameters())
            {

                MemberInfo? sourceMember = GetPropertyOrField(sourceType, parameter.Name);
                if (sourceMember == null || (sourceMember is PropertyInfo prop && !prop.CanRead))
                    throw new Exception($"{parameter.Name} parameter of constructor has no match or can't be read at {sourceType.Name} while mapping {sourceType.Name} to {destType.Name}. Consider using a different constructor or configure via mapper");

                MemberInfo? destMember = GetPropertyOrField(destType, parameter.Name);
                if (destMember == null || (destMember is PropertyInfo destProp && !destProp.CanRead))
                    throw new Exception($"{parameter.Name} parameter of constructor has no match or can't be read at {destType.Name} while mapping {sourceType.Name} to {destType.Name}. Consider using a different constructor or configure via mapper");

                var memberAccess = Expression.PropertyOrField(sourceParameter, parameter.Name);
                var sourceExprLambda = Expression.Lambda(memberAccess, sourceParameter);
                sourceExpr = new ParameterReplacer(sourceExprLambda.Parameters[0], sourceParameter).Visit(sourceExprLambda.Body);
                ctorArgExp = CreateExpressionForMember(destMember, sourceExpr);
                if (ctorArgExp == null)
                    ctorArgExp = Expression.Constant(null, sourceExpr.Type);

                arguments.Add(ctorArgExp);
            }

            return Expression.New(constructor, arguments);
        }

        [Obsolete]
        private ParameterExpression? FindParameterInBindings(IEnumerable<MemberBinding> bindings)
        {
            foreach (var binding in bindings)
            {
                if (binding is MemberAssignment assignment)
                {
                    var param = ExtractParameterExpression(assignment.Expression);
                    if (param != null)
                        return param;
                }
            }
            return null;
        }

        private MemberAssignment? CreateBinding(MemberInfo destMember, ParameterExpression sourceParameter, Type sourceType)
        {

            Expression sourceExpr;
            Type destinationType = destMember.DeclaringType!;
            MapperConfiguration? config;
            MapperMatching? matching;

            config = this._configurations.FirstOrDefault(x => x.SourceType == sourceType && x.DestinationType == destinationType);
            matching = config?.Matchings.FirstOrDefault(x => x.DestinationMember == destMember.Name);

            if (matching == default(MapperMatching))
            {
                MemberInfo? sourceMember = GetPropertyOrField(sourceType, destMember.Name);
                if (sourceMember == null || (sourceMember is PropertyInfo prop && !prop.CanRead))
                    return null;

                var memberAccess = Expression.PropertyOrField(sourceParameter, destMember.Name);
                var sourceExprLambda = Expression.Lambda(memberAccess, sourceParameter);
                sourceExpr = new ParameterReplacer(sourceExprLambda.Parameters[0], sourceParameter).Visit(sourceExprLambda.Body);

                if (config == null)
                {
                    config = new MapperConfiguration(sourceType, destinationType);
                    this._configurations.Add(config);
                }
                return CreateBindingCore(destMember, sourceExpr);
            }

            var lambdaExpr = matching!.Value.SourceExpr as LambdaExpression;
            sourceExpr = new ParameterReplacer(lambdaExpr!.Parameters[0], sourceParameter).Visit(lambdaExpr.Body);
            return CreateBindingCore(destMember, sourceExpr);
        }

        private MemberAssignment? CreateBindingCore(MemberInfo destMember, Expression sourceExpr)
        {
            Expression? expr = CreateExpressionForMember(destMember, sourceExpr);
            if (expr == null)
                return null;
            return Expression.Bind(destMember, expr);
        }

        private Expression? CreateExpressionForMember(MemberInfo destMember, Expression sourceExpr)
        {

            Type? destMemberType = GetMemberType(destMember)!;
            Type sourceExprType = GetReturnTypeFromExpression(sourceExpr);

            if (sourceExprType == destMemberType)
                return sourceExpr;

            Expression? resultExpr;

            // Handle collection members (IEnumerable)
            if (typeof(IEnumerable).IsAssignableFrom(destMemberType) && destMemberType != typeof(string))
            {
                resultExpr = CreateCollectionExpression(destMember, sourceExpr);
                return resultExpr;
            }

            // Handle complex types (e.g., nested classes)
            if (TypeHelper.IsActualClass(destMemberType) && TypeHelper.IsActualClass(sourceExprType))
            {
                // Null kontrolü yapalım
                var sourceIsNull = Expression.Equal(sourceExpr, Expression.Constant(null, sourceExprType));
                var nestedMapExpr = CreateMapExpression(sourceExprType, destMemberType);
                var mapExpression = Expression.Invoke(nestedMapExpr, sourceExpr);

                // Nullsa default value atanır
                resultExpr = Expression.Condition(
                    sourceIsNull,
                    Expression.Default(destMemberType),
                    mapExpression
                );

                return resultExpr;
            }

            #region (int, string, double, boolean, decimal, enum etc)

            if (IsSimpleType(sourceExprType, destMemberType))
            {
                // Check if the source and destination types are nullable
                var sourceTypeActual = Nullable.GetUnderlyingType(sourceExprType) ?? sourceExprType;
                var destTypeActual = Nullable.GetUnderlyingType(destMemberType) ?? destMemberType;

                // ctorArgumentExp is nullable
                if (sourceExprType != sourceTypeActual)
                {
                    var hasValue = Expression.Property(sourceExpr, "HasValue");
                    var getValueOrDefault = Expression.Property(sourceExpr, "Value");
                    var valueConversion = ConvertPrimitive(getValueOrDefault, destTypeActual);
                    resultExpr = Expression.Condition(
                        hasValue,
                        valueConversion,
                        Expression.Default(destMemberType)
                    );
                }
                else
                    resultExpr = ConvertPrimitive(sourceExpr, destTypeActual);

                return resultExpr;
            }

            #endregion

            return null;
        }

        private Expression ConvertPrimitive(Expression sourceExp, Type destType)
        {
            if (destType == typeof(string))
            {
                // Convert destinationMemberExpr string using ToString
                return Expression.Call(sourceExp, nameof(object.ToString), Type.EmptyTypes);
            }
            else if (sourceExp.Type == typeof(string))
            {
                // Convert sourceExp string destinationMemberExpr numeric type using Convert.ChangeType
                var convertMethod = typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) });
                var convertCall = Expression.Call(convertMethod, sourceExp, Expression.Constant(destType));
                return Expression.Convert(convertCall, destType);
            }
            else
            {
                // Handle other primitive type conversions
                return Expression.Convert(sourceExp, destType);
            }
        }

        private MemberInfo? GetPropertyOrField(Type type, string memberName)
        {
            var prop = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            if (prop != null)
                return prop;

            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            if (field != null)
                return field;

            return null;
        }

        private IEnumerable<MemberInfo> GetMembersWriteable(Type type)
        {
            return type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.SetField)
                .Where(m => m.MemberType == MemberTypes.Property || m.MemberType == MemberTypes.Field);
        }

        private Type? GetMemberType(MemberInfo member)
        {
            if (member is PropertyInfo destProp)
                return destProp.PropertyType;
            if (member is FieldInfo destField)
                return destField.FieldType;
            return null;
        }

        private ParameterExpression? ExtractParameterExpression(Expression expression)
        {
            // LambdaExpression ise, onun parametresine ulaşabiliriz
            if (expression is LambdaExpression lambdaExpression)
                return lambdaExpression.Parameters.FirstOrDefault();

            // MemberExpression içinde bir ParameterExpression olabilir (örneğin x.FirstName)
            if (expression is MemberExpression memberExpression)
                return ExtractParameterExpression(memberExpression.Expression);

            // MethodCallExpression'da metodun objesini inceleyebiliriz
            if (expression is MethodCallExpression methodCallExpression)
                return ExtractParameterExpression(methodCallExpression.Object);

            // BinaryExpression'da hem sol hem de sağ tarafı kontrol edebiliriz
            if (expression is BinaryExpression binaryExpression)
            {
                var leftParam = ExtractParameterExpression(binaryExpression.Left);
                if (leftParam != null)
                    return leftParam;
                return ExtractParameterExpression(binaryExpression.Right);
            }

            // Eğer doğrudan bir ParameterExpression ise, parametreyi döndür
            if (expression is ParameterExpression parameterExpression)
                return parameterExpression;

            // Eğer bir UnaryExpression (örneğin cast edilen bir ifade) varsa onu da inceleyelim
            if (expression is UnaryExpression unaryExpression)
                return ExtractParameterExpression(unaryExpression.Operand);
            return null;
        }

        private MemberAssignment? CreateCollectionBinding(MemberInfo destMember, Expression sourceExpr)
        {
            var expr = CreateCollectionExpression(destMember, sourceExpr);
            if (expr == null)
                return null;
            return Expression.Bind(destMember, expr);
        }

        private Expression? CreateCollectionExpression(MemberInfo destMember, Expression sourceExpr)
        {
            var type = GetReturnTypeFromExpression(sourceExpr);
            Type? destMemberType = GetMemberType(destMember);

            if (destMemberType == null)
                return null;

            // Get the element types of the source and destination collections
            var sourceElementType = type.GetGenericArguments().FirstOrDefault();
            var destElementType = destMemberType.GetGenericArguments().FirstOrDefault();

            if (sourceElementType != null && destElementType != null)
            {
                // Create the select ctorArgumentExp for the collection mapping
                var selectExpression = CreateSelectExpression(sourceElementType, destElementType, sourceExpr);

                // Handle specific collection types like List, Array, ICollection, etc.
                Expression convertedCollection;

                if (destMemberType.IsArray)
                {
                    // Convert destinationMemberExpr array
                    var toArrayMethod = typeof(Enumerable).GetMethod("ToArray").MakeGenericMethod(destElementType);
                    convertedCollection = Expression.Call(toArrayMethod, selectExpression);
                }
                else if (typeof(ICollection<>).MakeGenericType(destElementType).IsAssignableFrom(destMemberType))
                {
                    // Convert destinationMemberExpr ICollection
                    var toListMethod = typeof(Enumerable).GetMethod("ToList").MakeGenericMethod(destElementType);
                    var toListCall = Expression.Call(toListMethod, selectExpression);
                    convertedCollection = Expression.Convert(toListCall, destMemberType);
                }
                else if (typeof(IEnumerable<>).MakeGenericType(destElementType).IsAssignableFrom(destMemberType))
                    convertedCollection = selectExpression;
                else
                    convertedCollection = selectExpression;

                return convertedCollection;
            }

            return null;
        }

        private Expression CreateSelectExpression(Type sourceElementType, Type destElementType, Expression sourceExpr)
        {
            // sourceExp null ise null döndürmek için null kontrolü yapıyoruz
            var sourceIsNull = Expression.ReferenceEqual(sourceExpr, Expression.Constant(null));

            // Null durumda null IEnumerable<T> döndür
            var nullValue = Expression.Constant(null, typeof(IEnumerable<>).MakeGenericType(destElementType));

            // Eğer sourceExp null ise null döndürelim
            var conditionExpr = Expression.Condition(
                sourceIsNull,
                nullValue,  // Null durumda null döndür
                CreateSelectCall(sourceExpr, sourceElementType, destElementType) // Aksi halde Where + Select işlemi
            );

            return conditionExpr;
        }

        private Expression CreateSelectCall(Expression sourceExpr, Type sourceElementType, Type destElementType)
        {
            // Koleksiyonun her bir öğesi için map işlemi
            var sourceElementParameter = Expression.Parameter(sourceElementType, sourceElementType.Name);

            var elementIsNotNull = Expression.NotEqual(sourceElementParameter, Expression.Constant(null, sourceElementType));

            // Null olan elemanları elemek için Where filtresi ekliyoruz
            var whereLambda = Expression.Lambda(elementIsNotNull, sourceElementParameter);

            var whereMethod = typeof(Enumerable).GetMethods()
                .Where(m => m.Name == "Where" && m.GetParameters().Length == 2)
                .Where(m =>
                {
                    var parameters = m.GetParameters();

                    // İlk parametre IEnumerable<TSource> mi?
                    var firstParam = parameters[0].ParameterType;
                    if (!firstParam.IsGenericType || firstParam.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                        return false;

                    // İkinci parametre Func<TSource, bool> mi?
                    var secondParam = parameters[1].ParameterType;
                    if (!secondParam.IsGenericType || secondParam.GetGenericTypeDefinition() != typeof(Func<,>))
                        return false;

                    var funcArgs = secondParam.GetGenericArguments();

                    // Func<TSource, bool> olduğuna emin olalım
                    return funcArgs.Length == 2 && funcArgs[0] == firstParam.GetGenericArguments()[0] && funcArgs[1] == typeof(bool);
                })
                .Single()
                .MakeGenericMethod(sourceElementType);

            var whereCall = Expression.Call(whereMethod, sourceExpr, whereLambda);

            // Mapping ctorArgumentExp
            var mapExpression = CreateMapExpression(sourceElementType, destElementType);
            var selectBody = Expression.Invoke(mapExpression, sourceElementParameter);
            var selectLambda = Expression.Lambda(selectBody, sourceElementParameter);

            var selectMethod = typeof(Enumerable).GetMethods()
                .Where(m =>
                {
                    if (m.Name != "Select" || m.GetParameters().Length != 2)
                        return false;
                    var parameters = m.GetParameters();
                    var selectorType = parameters[1].ParameterType;
                    return selectorType.IsGenericType && selectorType.GetGenericArguments().Length == 2;
                })
                .Single()
                .MakeGenericMethod(sourceElementType, destElementType);

            // Select işlemi
            var selectCall = Expression.Call(selectMethod, whereCall, selectLambda);
            return selectCall;
        }

        private bool IsSimpleType(Type sourceType, Type destType)
        {
            // Check if both types are either primitive or string
            return IsSimpleType(sourceType) && IsSimpleType(destType);
        }

        private bool IsSimpleType(Type type)
        {
            var checkedtype = Nullable.GetUnderlyingType(type) ?? type;
            return checkedtype.IsPrimitive || checkedtype == typeof(string) || checkedtype.IsEnum;
        }

        private Type GetReturnTypeFromExpression(Expression expression)
        {
            return ExpressionHelper.GetReturnTypeFromExpression(expression);
        }

    }

    internal class MapperConfiguration
    {
        internal readonly Type SourceType;
        internal readonly Type DestinationType;

        public MapperConfiguration(Type sourceType, Type destinationType)
        {
            SourceType = sourceType;
            DestinationType = destinationType;
        }

        internal List<MapperMatching> Matchings { get; set; } = new();
        internal MemberInitExpression? MemberInitExpression { get; private set; }
        internal NewExpression? CtorExpression { get; private set; }

        internal void SetMemberInitExpression(MemberInitExpression memberInitExpression)
        {
            MemberInitExpression = memberInitExpression;
        }

        internal void SetCtorExpression(NewExpression ctorExpression)
        {
            CtorExpression = ctorExpression;
        }
    }

    public class MapperConfiguration<TSource, TDestination> : IDisposable where TSource : class where TDestination : class
    {
        private bool disposedValue;

        internal List<MapperMatching> Matchings { get; set; } = new();
        internal NewExpression? CtorExpression { get; set; }

        public MapperConfiguration<TSource, TDestination> Match<TSourceExpression, TDestinationMember>(Expression<Func<TSource, TSourceExpression>> sourceExpr, Expression<Func<TDestination, TDestinationMember>> destinationMemberExpr)
        {
            // sourceExp'ı kontrol et
            if (sourceExpr.Body is UnaryExpression sourceBody && sourceBody.NodeType == ExpressionType.Convert)
            {
                sourceExpr = Expression.Lambda<Func<TSource, TSourceExpression>>(sourceBody.Operand, sourceExpr.Parameters);
            }

            // destinationMemberExpr'i kontrol et
            if (destinationMemberExpr.Body is UnaryExpression destinationBody && destinationBody.NodeType == ExpressionType.Convert)
            {
                destinationMemberExpr = Expression.Lambda<Func<TDestination, TDestinationMember>>(destinationBody.Operand, destinationMemberExpr.Parameters);
            }

            if (destinationMemberExpr.Body is MemberExpression destinationMember)
            {
                if (destinationMember.Member.MemberType == MemberTypes.Property || destinationMember.Member.MemberType == MemberTypes.Field)
                {
                    Matchings.RemoveAll(x => x.DestinationMember == destinationMember.Member.Name);
                    //var matching = new MapperMatching(destinationMember.Member.Name, sourceExpr);
                    Matchings.Add(new MapperMatching(destinationMember.Member.Name, sourceExpr));
                    return this;
                }

                throw new Exception($"Ensure you are using properties or fields while mapping to {typeof(TDestination).Name}");
            }

            throw new Exception($"Ensure all destination expressions represent properties or fields while converting {typeof(TSource).Name} to {typeof(TDestination).Name}");
        }

        /// <summary>
        /// Allows which public constructor to use for <typeparamref name="TDestination"/>.
        /// </summary>
        /// <param name="ctorExp"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public MapperConfiguration<TSource, TDestination> UsingPublicConstructor(Expression<Func<TSource, TDestination>> ctorExp)
        {

            if (ctorExp.Body is NewExpression newExp)
            {
                CtorExpression = newExp;
                return this;
            }
            throw new InvalidOperationException($"{nameof(UsingPublicConstructor)} method should return a constructor");
        }

        /// <summary>
        /// Uses the <typeparamref name="TDestination"/> constructor matching types of arguments via this method.
        /// </summary>
        /// <param name="ctorBuilderAction"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public MapperConfiguration<TSource, TDestination> UsingNonPublicConstructor(Expression<Func<TSource, ParameterContainer>> ctorExp)
        {
            if (ctorExp.Body is NewExpression newExp)
            {
                //this.CtorArgExpressions.Clear();
                NewArrayExpression argumentsArrayExp = (newExp.Arguments.First() as NewArrayExpression)!;
                List<Expression> argumentExpressions = new();

                foreach (var expression in argumentsArrayExp.Expressions)
                {
                    if (expression is UnaryExpression unaryExp && unaryExp.NodeType == ExpressionType.Convert && unaryExp.Type == typeof(object))
                        argumentExpressions.Add(unaryExp.Operand);
                    else
                        argumentExpressions.Add(expression);
                }

                //this.CtorArgExpressions.AddRange(argumentExpressions);
                BuildConstructorWithArguments(argumentExpressions);
                return this;
            }

            throw new InvalidOperationException($"{nameof(UsingNonPublicConstructor)} method should return a constructor of {nameof(ParameterContainer)} type");
        }

        private void BuildConstructorWithArguments(List<Expression> argumentExpressions)
        {
            Type[] types = Type.EmptyTypes;

            if (argumentExpressions.Count > 0)
                types = argumentExpressions.Select(x => x.Type).ToArray();

            ConstructorInfo? ctor = typeof(TDestination).GetConstructor(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic, types);
            if (ctor == null)
                throw new InvalidOperationException($"{typeof(TDestination).Name} type has no constructor with the types of arguments you passed");

            CtorExpression = Expression.New(ctor, argumentExpressions);

            if (CtorExpression == null)
                throw new InvalidOperationException($"Ensure right parameter types are used while configuring constructor for {typeof(TDestination).Name}. Error occurred while mapping from {typeof(TSource).Name} to {typeof(TDestination).Name}");
        }

        #region Dispose

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    Matchings.Clear();
                    CtorExpression = null;
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields destinationMemberExpr null
                disposedValue = true;
            }
        }

        // //  override finalizer only if 'Dispose(bool disposing)' has code destinationMemberExpr free unmanaged resources
        // ~MapperConfiguration()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }

    internal record struct MapperMatching(string DestinationMember, Expression SourceExpr);

    public class ParameterContainer
    {
        public ParameterContainer(params object[] expressions)
        {

        }
    }
}