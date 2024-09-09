
namespace QueryMapper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public class QueryMapper
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

        public IQueryable<TDestination> Map<TSource, TDestination>(IQueryable<TSource> sourceQuery)
        {
            var selector = CreateMapExpression<TSource, TDestination>();
            return sourceQuery.Select(selector);
        }

        private Expression<Func<TSource, TDestination>> CreateMapExpression<TSource, TDestination>()
        {
            var sourceParameter = Expression.Parameter(typeof(TSource), nameof(TSource));
            var body = CreateMapExpressionCore(typeof(TSource), typeof(TDestination), sourceParameter);
            return Expression.Lambda<Func<TSource, TDestination>>(body, sourceParameter);
        }

        private Expression CreateNestedMapExpression(Type sourceType, Type destinationType, PropertyInfo sourceProp, Expression sourceParameter)
        {
            MemberExpression nestedSourceParameter = Expression.Property(sourceParameter, sourceProp);
            return CreateMapExpressionCore(sourceType, destinationType, nestedSourceParameter);
        }

        private Expression CreateNestedMapExpression(Type sourceType, Type destinationType, Expression sourceExpr)
        {
            return CreateMapExpressionCore(sourceType, destinationType, sourceExpr);
        }

        private LambdaExpression CreateMapExpression(Type sourceType, Type destType)
        {
            var sourceParameter = Expression.Parameter(sourceType, sourceType.Name);
            var body = CreateMapExpressionCore(sourceType, destType, sourceParameter);
            var lambdaType = typeof(Func<,>).MakeGenericType(sourceType, destType);
            return Expression.Lambda(lambdaType, body, sourceParameter);
        }

        private MemberInitExpression CreateMapExpressionCore(Type sourceType, Type destType, Expression sourceParameter)
        {
            var bindings = destType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(destProp => destProp.CanWrite)
                .Select(destProp => CreateBinding(destProp, sourceParameter, sourceType))
                .Where(binding => binding != null);

            return Expression.MemberInit(Expression.New(destType), bindings);
        }

        private MemberAssignment? CreateBinding(PropertyInfo destProp, Expression sourceParameter, Type sourceType)
        {

            #region MapperConfiguration

            Type? destinationType = destProp.DeclaringType;
            if (destinationType != null)
            {
                MapperConfiguration? config = this._configurations.FirstOrDefault(x => x.SourceType == sourceType && x.DestinationType == destinationType);
                if (config != null)
                {
                    MapperMatching? matching = config.Matchings.FirstOrDefault(x => x.DestinationProperty == destProp.Name);
                    if (matching != null)
                    {
                        // Yeni gövdeyi elde et
                        Expression sourceExpr = new ParameterReplacer((matching.SourceExpr as LambdaExpression).Parameters[0], sourceParameter).Visit((matching.SourceExpr as LambdaExpression).Body);

                        //Expression sourceExpr = matching.SourceExpr;
                        var sourceExprType = GetReturnTypeFromExpression(sourceExpr);

                        // Handle collection properties (IEnumerable)
                        if (typeof(IEnumerable).IsAssignableFrom(destProp.PropertyType) && destProp.PropertyType != typeof(string))
                            return CreateCollectionBinding(destProp, sourceExpr);

                        // Handle complex types (e.g., nested classes)
                        if (IsComplexType(destProp.PropertyType) && IsComplexType(sourceExprType))
                        {
                            var nestedMapExpression = CreateNestedMapExpression(sourceType, destProp.PropertyType, sourceExpr);
                            return Expression.Bind(destProp, nestedMapExpression);
                        }

                        #region Primitive types (int, string, double, boolean etc)

                        if (sourceExprType == destProp.PropertyType)
                            return Expression.Bind(destProp, sourceExpr);

                        if (IsPrimitiveOrString(sourceExprType, destProp.PropertyType))
                        {
                            // Check if the source and destination types are nullable
                            var sourceTypeChecked = Nullable.GetUnderlyingType(sourceExprType) ?? sourceExprType;
                            var destType = Nullable.GetUnderlyingType(destProp.PropertyType) ?? destProp.PropertyType;

                            Expression convertedValue;

                            if (sourceExprType != sourceTypeChecked)
                            {
                                var hasValue = Expression.Property(sourceExpr, "HasValue");
                                var getValueOrDefault = Expression.Property(sourceExpr, "Value");
                                var valueConversion = ConvertPrimitive(getValueOrDefault, destType);
                                convertedValue = Expression.Condition(
                                    hasValue,
                                    valueConversion,
                                    Expression.Default(destProp.PropertyType)
                                );
                            }
                            else
                                convertedValue = ConvertPrimitive(sourceExpr, destType);

                            return Expression.Bind(destProp, convertedValue);
                        }

                        #endregion
                    }
                }
            }

            #endregion

            var sourceProp = sourceType.GetProperty(destProp.Name);

            if (sourceProp == null || !sourceProp.CanRead)
                return null;

            // Handle collection properties (IEnumerable)
            if (typeof(IEnumerable).IsAssignableFrom(destProp.PropertyType) && destProp.PropertyType != typeof(string))
                return CreateCollectionBinding(destProp, sourceProp, sourceParameter);

            // Handle complex types (e.g., nested classes)
            if (IsComplexType(destProp.PropertyType) && IsComplexType(sourceProp.PropertyType))
            {
                var nestedMapExpression = CreateNestedMapExpression(sourceProp.PropertyType, destProp.PropertyType, sourceProp, sourceParameter);
                return Expression.Bind(destProp, nestedMapExpression);
            }

            #region Primitive types (int, string, double, boolean etc)

            if (sourceProp.PropertyType == destProp.PropertyType)
            {
                var sourceValue = Expression.Property(sourceParameter, sourceProp);
                return Expression.Bind(destProp, sourceValue);
            }

            if (IsPrimitiveOrString(sourceProp.PropertyType, destProp.PropertyType))
            {
                var sourceValue = Expression.Property(sourceParameter, sourceProp);
                // Check if the source and destination types are nullable
                var sourceTypeChecked = Nullable.GetUnderlyingType(sourceProp.PropertyType) ?? sourceProp.PropertyType;
                var destType = Nullable.GetUnderlyingType(destProp.PropertyType) ?? destProp.PropertyType;

                Expression convertedValue;

                if (sourceProp.PropertyType != sourceTypeChecked)
                {
                    var hasValue = Expression.Property(sourceValue, "HasValue");
                    var getValueOrDefault = Expression.Property(sourceValue, "Value");
                    var valueConversion = ConvertPrimitive(getValueOrDefault, destType);
                    convertedValue = Expression.Condition(
                        hasValue,
                        valueConversion,
                        Expression.Default(destProp.PropertyType)
                    );
                }
                else
                    convertedValue = ConvertPrimitive(sourceValue, destType);

                return Expression.Bind(destProp, convertedValue);
            }

            #endregion

            return null;
        }

        private Expression ConvertPrimitive(Expression sourceValue, Type destType)
        {
            if (destType == typeof(string))
            {
                // Convert destinationPropExpr string using ToString
                return Expression.Call(sourceValue, nameof(object.ToString), Type.EmptyTypes);
            }
            else if (sourceValue.Type == typeof(string))
            {
                // Convert sourceExpr string destinationPropExpr numeric type using Convert.ChangeType
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

        private MemberAssignment? CreateCollectionBinding(PropertyInfo destProp, PropertyInfo sourceProp, Expression sourceParameter)
        {
            var sourceValue = Expression.Property(sourceParameter, sourceProp);
            return CreateCollectionBinding(destProp, sourceValue);
        }

        private MemberAssignment? CreateCollectionBinding(PropertyInfo destProp, Expression sourceExpr)
        {
            var type = GetReturnTypeFromExpression(sourceExpr);

            // Get the element types of the source and destination collections
            var sourceElementType = type.GetGenericArguments().FirstOrDefault();
            var destElementType = destProp.PropertyType.GetGenericArguments().FirstOrDefault();

            if (sourceElementType != null && destElementType != null)
            {
                // Create the select expression for the collection mapping
                var selectExpression = CreateSelectExpression(sourceElementType, destElementType, sourceExpr);

                // Handle specific collection types like List, Array, ICollection, etc.
                Expression convertedCollection;
                if (destProp.PropertyType.IsArray)
                {
                    // Convert destinationPropExpr array
                    var toArrayMethod = typeof(Enumerable).GetMethod("ToArray").MakeGenericMethod(destElementType);
                    convertedCollection = Expression.Call(toArrayMethod, selectExpression);
                }
                else if (typeof(ICollection<>).MakeGenericType(destElementType).IsAssignableFrom(destProp.PropertyType))
                {
                    // Convert destinationPropExpr ICollection
                    var toListMethod = typeof(Enumerable).GetMethod("ToList").MakeGenericMethod(destElementType);
                    var toListCall = Expression.Call(toListMethod, selectExpression);
                    convertedCollection = Expression.Convert(toListCall, destProp.PropertyType);
                }
                else if (typeof(IEnumerable<>).MakeGenericType(destElementType).IsAssignableFrom(destProp.PropertyType))
                {
                    // Convert destinationPropExpr IEnumerable (no further conversion needed)
                    convertedCollection = selectExpression;
                }
                else
                {
                    // Handle other cases, like custom collection types
                    convertedCollection = selectExpression;
                }

                return Expression.Bind(destProp, convertedCollection);
            }

            return null;
        }

        private Expression CreateSelectExpression(Type sourceElementType, Type destElementType, Expression sourceValue)
        {
            var sourceElementParameter = Expression.Parameter(sourceElementType, "item");

            // Recursively map the collection elements
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

            return Expression.Call(selectMethod, sourceValue, selectLambda);
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
                    {
                        return propertyInfo.PropertyType;
                    }
                    if (memberExpression.Member is FieldInfo fieldInfo)
                    {
                        return fieldInfo.FieldType;
                    }
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
                    {
                        return leftType;
                    }

                    // If not, return the most specific common type, like object
                    return typeof(object);

                    // Handle other types of expressions if needed...
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

    }

    public class MapperConfiguration<TSource, TDestination> : IDisposable where TSource : class
        where TDestination : class
    {
        private bool disposedValue;

        internal List<MapperMatching> Matchings { get; set; } = new();

        public void Match(Expression<Func<TSource, object>> sourceExpr, Expression<Func<TDestination, object>> destinationPropExpr)
        {
            if (destinationPropExpr.Body is MemberExpression destinationMemberExpr)
            {
                if (destinationMemberExpr.Member is PropertyInfo prop)
                {

                    Matchings.RemoveAll(x => x.DestinationProperty == prop.Name);
                    var instance = new MapperMatching(prop.Name, sourceExpr);
                    Matchings.Add(instance);
                    return;
                }

                throw new Exception($"Ensure you are using properties while mapping to {typeof(TDestination).Name}");
            }
            throw new Exception($"Ensure all TO expressions represent properties while converting {typeof(TSource).Name} to {typeof(TDestination).Name}");
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
                // TODO: set large fields destinationPropExpr null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code destinationPropExpr free unmanaged resources
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

        public MapperMatching(string destinationProperty, Expression sourceExpr)
        {
            DestinationProperty = destinationProperty;
            SourceExpr = sourceExpr;
        }

        public string DestinationProperty;
        public Expression SourceExpr;
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
