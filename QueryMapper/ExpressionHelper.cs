using System.Linq.Expressions;
using System.Reflection;

namespace QueryMapper
{
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
}
