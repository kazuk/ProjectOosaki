using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace Oosaki.Msil.Extentions
{
    public static class ReflectionEx
    {
        [Pure]
        public static MethodInfo GetMethod<TDelegate>(this Expression<TDelegate> lambda)
        {
            Contract.Requires(lambda != null);
            Contract.Requires(IsMethodCall(lambda));

            var callExpression = lambda.Body as MethodCallExpression;
            // ReSharper disable PossibleNullReferenceException
            return callExpression.Method;
            // ReSharper restore PossibleNullReferenceException
        }

        [Pure]
        public static bool IsMethodCall<TDelegate>(Expression<TDelegate> lambda)
        {
            Contract.Requires(lambda != null);

            return lambda.Body is MethodCallExpression;
        }
    }
}