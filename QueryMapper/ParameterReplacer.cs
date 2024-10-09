using System.Linq.Expressions;

namespace QueryMapper
{
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
