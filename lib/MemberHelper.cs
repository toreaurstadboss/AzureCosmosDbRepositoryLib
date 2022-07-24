using System.Linq.Expressions;

namespace AzureCosmosDbRepositoryLib
{
    public static class MemberHelper
    {


        public static string GetMemberName<TForm, TProperty>(this Expression<Func<TForm, TProperty>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return GetMemberName(expression.Body);
        }


        public static string GetMemberName<T>(this Expression<Func<T>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return GetMemberName(expression.Body);
        }

        private static string GetMemberName(this Expression expression)
        {
            if (expression == null)
            {
                return "";
            }

            switch (expression.NodeType)
            {
                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)expression;

                    var supername = memberExpression.Expression != null ? GetMemberName(memberExpression.Expression) : null;

                    if (string.IsNullOrWhiteSpace(supername))
                    {
                        return memberExpression.Member.Name;
                    }

                    return string.Concat(supername, '.', memberExpression.Member.Name);

                case ExpressionType.Call:
                    var callExpression = (MethodCallExpression)expression;
                    return callExpression.Method.Name;

                case ExpressionType.Convert:
                    var unaryExpression = (UnaryExpression)expression;
                    return GetMemberName(unaryExpression.Operand);

                case ExpressionType.Constant:
                case ExpressionType.Parameter:
                    return string.Empty;

                default:
                    throw new ArgumentException("The expression is not a member access or method call expression");
            }
        }

    }
}
