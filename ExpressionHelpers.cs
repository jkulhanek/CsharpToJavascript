using Forcoft.Javascript.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Forcoft.Helpers.Expressions
{
    public static class ExpressionHelpers
    {
        public static object ExpressionValue(Expression expr)
        {
            if (expr is MemberExpression)
            {
                var memexp = (expr as MemberExpression);
                if (memexp.Member is FieldInfo)
                {
                    return (memexp.Member as FieldInfo).GetValue(ExpressionValue(memexp.Expression));
                }
                if (memexp.Member is PropertyInfo)
                {
                    return (memexp.Member as PropertyInfo).GetValue(ExpressionValue(memexp.Expression),null);
                }
            }
            if (expr is ConstantExpression)
            {
                return (expr as ConstantExpression).Value;
            }
            if (expr is UnaryExpression)
            {
                if (expr.NodeType == ExpressionType.Convert)
                {
                    return TypeHelper.ToOrDefault(ExpressionValue((expr as UnaryExpression).Operand) as IConvertible, (expr as UnaryExpression).Type);
                }
            }
            throw new Exception("Expression is not supported");
        }
    }
}
