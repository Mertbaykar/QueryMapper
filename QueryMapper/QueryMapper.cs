
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

        private readonly List<MapperConfiguration> _configurations = new();

        public QueryMapper Configure<TSource, TDestination>(Action<MapperConfiguration<TSource, TDestination>>? configuration = null)
            where TSource : class
            where TDestination : class
        {
            // remove definition if previous exists
            _configurations.RemoveAll(c => c.SourceType == typeof(TSource) && c.DestinationType == typeof(TDestination));

            var config = new MapperConfiguration(typeof(TSource), typeof(TDestination));

            using (var configTemp = new MapperConfiguration<TSource, TDestination>())
            {
                configuration?.Invoke(configTemp);
                configTemp.Matchings.ForEach(config.Matchings.Add);
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

            if (config != null && config.MemberInitExpression != null)
            {
                // mevcut parametreyi yenisiyle değiştir
                var oldParameter = FindParameterInBindings(config.MemberInitExpression.Bindings)!;
                var replacer = new ParameterReplacer(oldParameter, sourceParameter);
                // binding'ler yeni parametre ile yenileniyor
                var newBindings = config.MemberInitExpression.Bindings.Select(binding =>
                {
                    if (binding is MemberAssignment assignment)
                    {
                        var newExpression = replacer.Visit(assignment.Expression);
                        return Expression.Bind(assignment.Member, newExpression);
                    }
                    return binding;
                });

                var newExpression = Expression.MemberInit(Expression.New(destType), newBindings);
                return newExpression;
            }

            var bindings =
                 GetMembersOfType(destType)
                .Select(destMember => CreateBinding(destMember, sourceParameter, sourceType))
                .Where(binding => binding != null);

            MemberInitExpression mapExpr = Expression.MemberInit(Expression.New(destType), bindings);

            if (config == null)
                config = this._configurations.First(x => x.SourceType == sourceType && x.DestinationType == destType);

            config.SetMemberInitExpression(mapExpr);
            return mapExpr;
        }

        private ParameterExpression? FindParameterInBindings(IEnumerable<MemberBinding> bindings)
        {
            foreach (var binding in bindings)
            {
                if (binding is MemberAssignment assignment)
                {
                    if (assignment.Expression is ParameterExpression parameter)
                        return parameter;

                    var param = FindParameterInExpression(assignment.Expression);
                    if (param != null)
                        return param;
                }
            }
            return null;
        }

        private ParameterExpression? FindParameterInExpression(Expression expr)
        {
            if (expr is ParameterExpression parameter)
                return parameter;
            if (expr is MemberExpression memberExpr)
                return FindParameterInExpression(memberExpr.Expression);
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

            if (matching == null)
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

            var lambdaExpr = matching.SourceExpr as LambdaExpression;
            sourceExpr = new ParameterReplacer(lambdaExpr.Parameters[0], sourceParameter).Visit(lambdaExpr.Body);
            return CreateBindingCore(destMember, sourceExpr);
        }

        private MemberAssignment? CreateBindingCore(MemberInfo destMember, Expression sourceExpr)
        {

            Type? destMemberType = GetMemberType(destMember)!;
            Type sourceExprType = GetReturnTypeFromExpression(sourceExpr);
            // Handle collection members (IEnumerable)
            if (typeof(IEnumerable).IsAssignableFrom(destMemberType) && destMemberType != typeof(string))
                return CreateCollectionBinding(destMember, sourceExpr);

            // Handle complex types (e.g., nested classes)
            if (IsComplexType(destMemberType) && IsComplexType(sourceExprType))
            {
                // Null kontrolü yapalım
                var sourceIsNull = Expression.Equal(sourceExpr, Expression.Constant(null, sourceExprType));
                var nestedMapExpr = CreateMapExpression(sourceExprType, destMemberType);
                var mapExpression = Expression.Invoke(nestedMapExpr, sourceExpr);

                // Nullsa default value atanması (null olmalı)
                var conditionExpr = Expression.Condition(
                    sourceIsNull,
                    Expression.Default(destMemberType),
                    mapExpression
                );

                return Expression.Bind(destMember, conditionExpr);
            }

            #region Primitive types (int, string, double, boolean etc)

            if (sourceExprType == destMemberType)
                return Expression.Bind(destMember, sourceExpr);

            if (IsPrimitiveOrString(sourceExprType, destMemberType))
            {
                // Check if the source and destination types are nullable
                var sourceTypeChecked = Nullable.GetUnderlyingType(sourceExprType) ?? sourceExprType;
                var destType = Nullable.GetUnderlyingType(destMemberType) ?? destMemberType;

                Expression convertedValue;

                if (sourceExprType != sourceTypeChecked)
                {
                    var hasValue = Expression.Property(sourceExpr, "HasValue");
                    var getValueOrDefault = Expression.Property(sourceExpr, "Value");
                    var valueConversion = ConvertPrimitive(getValueOrDefault, destType);
                    convertedValue = Expression.Condition(
                        hasValue,
                        valueConversion,
                        Expression.Default(destMemberType)
                    );
                }
                else
                    convertedValue = ConvertPrimitive(sourceExpr, destType);

                return Expression.Bind(destMember, convertedValue);
            }

            #endregion

            return null;
        }

        private Expression ConvertPrimitive(Expression sourceValue, Type destType)
        {
            if (destType == typeof(string))
            {
                // Convert destinationMemberExpr string using ToString
                return Expression.Call(sourceValue, nameof(object.ToString), Type.EmptyTypes);
            }
            else if (sourceValue.Type == typeof(string))
            {
                // Convert sourceExpr string destinationMemberExpr numeric type using Convert.ChangeType
                var convertMethod = typeof(Convert).GetMethod(nameof(Convert.ChangeType), new[] { typeof(object), typeof(Type) });
                var convertCall = Expression.Call(convertMethod, sourceValue, Expression.Constant(destType));
                return Expression.Convert(convertCall, destType);
            }
            else
            {
                // Handle other primitive type conversions
                return Expression.Convert(sourceValue, destType);
            }
        }

        private MemberInfo? GetPropertyOrField(Type type, string memberName)
        {
            var prop = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (prop != null)
                return prop;

            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
                return field;

            return null;
        }

        private MemberInfo[] GetMembersOfType(Type type)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanWrite).OfType<MemberInfo>();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance).OfType<MemberInfo>();
            return props.Concat(fields).ToArray();
        }

        private Type? GetMemberType(MemberInfo member)
        {
            if (member is PropertyInfo destProp)
                return destProp.PropertyType;
            else if (member is FieldInfo destField)
                return destField.FieldType;
            else
                return null;
        }

        private MemberAssignment? CreateCollectionBinding(MemberInfo destMember, Expression sourceExpr)
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
                // Create the select expression for the collection mapping
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

                return Expression.Bind(destMember, convertedCollection);
            }

            return null;
        }

        private Expression CreateSelectExpression(Type sourceElementType, Type destElementType, Expression sourceExpr)
        {
            // Parametre olarak gelen sourceExpr null olabilir, bunu kontrol edelim
            var sourceIsNull = Expression.Equal(sourceExpr, Expression.Constant(null, sourceExpr.Type));

            // Null kontrolü ve boş IEnumerable<T> döndürme işlemi
            var emptyEnumerableMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Empty))
                .MakeGenericMethod(destElementType);
            var emptyEnumerable = Expression.Call(emptyEnumerableMethod);

            // Eğer sourceExpr null ise boş IEnumerable<T> döndürelim
            var conditionExpr = Expression.Condition(
                sourceIsNull,
                emptyEnumerable,  // Null durumda boş IEnumerable<T> döndür
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

            // Mapping expression
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

        private bool IsComplexType(Type type)
        {
            return type.IsClass && type != typeof(string);
        }

        private bool IsPrimitiveOrString(Type sourceType, Type destType)
        {
            // Check if both types are either primitive or string
            return (IsPrimitiveOrString(sourceType) && IsPrimitiveOrString(destType));
        }

        private bool IsPrimitiveOrString(Type type)
        {
            var checkedtype = Nullable.GetUnderlyingType(type) ?? type;
            return checkedtype.IsPrimitive || checkedtype == typeof(string);
        }

        private Type GetReturnTypeFromExpression(Expression expression)
        {
            return ExpressionHelper.GetReturnTypeFromExpression(expression);
        }

    }

    internal class ExpressionHelper
    {
        public static Type GetReturnTypeFromExpression(Expression expression)
        {
            switch (expression)
            {
                // Handle lambda expressions
                case LambdaExpression lambdaExpression:
                    return GetReturnTypeFromExpression(lambdaExpression.Body);

                // Handle method calls
                case MethodCallExpression methodCallExpression:
                    return methodCallExpression.Method.ReturnType;

                // Handle member access (e.g., accessing properties or fields)
                case MemberExpression memberExpression:
                    if (memberExpression.Member is PropertyInfo propertyInfo)
                        return propertyInfo.PropertyType;
                    if (memberExpression.Member is FieldInfo fieldInfo)
                        return fieldInfo.FieldType;
                    break;

                // Handle unary expressions (e.g., type conversions)
                case UnaryExpression unaryExpression:
                    return unaryExpression.Type;

                // Handle constant expressions (e.g., literal values)
                case ConstantExpression constantExpression:
                    return constantExpression.Type;

                // Handle binary expressions (e.g., addition, concatenation)
                case BinaryExpression binaryExpression:
                    // In the case of concatenation, both sides should be strings
                    // We can assume the result will be of the same type as the operands
                    Type leftType = GetReturnTypeFromExpression(binaryExpression.Left);
                    Type rightType = GetReturnTypeFromExpression(binaryExpression.Right);

                    // If both operands are of the same type, return that type
                    if (leftType == rightType)
                        return leftType;

                    // If not, return the most specific common type, like object
                    return typeof(object);
            }

            throw new InvalidOperationException("Unsupported expression type");
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
        internal MemberInitExpression MemberInitExpression { get; private set; }

        internal void SetMemberInitExpression(MemberInitExpression memberInitExpression)
        {
            MemberInitExpression = memberInitExpression;
        }
    }

    public class MapperConfiguration<TSource, TDestination> : IDisposable where TSource : class
        where TDestination : class
    {
        private bool disposedValue;

        internal List<MapperMatching> Matchings { get; set; } = new();

        public void Match(Expression<Func<TSource, object>> sourceExpr, Expression<Func<TDestination, object>> destinationMemberExpr)
        {
            if (destinationMemberExpr.Body is MemberExpression destinationMember)
            {
                if (destinationMember.Member.MemberType == MemberTypes.Property || destinationMember.Member.MemberType == MemberTypes.Field)
                {
                    Matchings.RemoveAll(x => x.DestinationMember == destinationMember.Member.Name);
                    var matching = new MapperMatching(destinationMember.Member.Name, sourceExpr);
                    Matchings.Add(matching);
                    return;
                }

                throw new Exception($"Ensure you are using properties or fields while mapping to {typeof(TDestination).Name}");
            }
            throw new Exception($"Ensure all destination expressions represent properties or fields while converting {typeof(TSource).Name} to {typeof(TDestination).Name}");
        }

        #region Dispose

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    Matchings.Clear();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields destinationMemberExpr null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code destinationMemberExpr free unmanaged resources
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

    internal class MapperMatching
    {

        public MapperMatching(string destinationMember, Expression sourceExpr)
        {
            DestinationMember = destinationMember;
            SourceExpr = sourceExpr;
        }

        public readonly string DestinationMember;
        public readonly Expression SourceExpr;
    }

    internal class ParameterReplacer : ExpressionVisitor
    {
        private readonly Expression _oldExpression;
        private readonly Expression _newExpression;

        public ParameterReplacer(Expression oldExpression, Expression newExpression)
        {
            _oldExpression = oldExpression;
            _newExpression = newExpression;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldExpression ? _newExpression : base.VisitParameter(node);
        }
    }
}