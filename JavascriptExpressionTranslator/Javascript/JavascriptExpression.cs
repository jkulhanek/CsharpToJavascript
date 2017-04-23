using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Forcoft.Extensions.Enumerable;
using System.Reflection;

namespace Forcoft.Javascript
{
    /// <summary>
    /// A type representing expression which can be converted to javascript
    /// </summary>
    /// <example>
    /// LambdaExpression exp=...;
    /// JavascriptExpression jexp=exp;
    /// string javascriptFunction=jexp.Compile();
    /// </example>
    public sealed class JavascriptExpression
    {
        protected System.Linq.Expressions.LambdaExpression _expr { get; set; }

        public Func<MemberInfo,bool, string> MemberReplacement { get; set; }

        private bool _simpleMember { get; set; }

        public bool IsInline { get; set; }
        /// <summary>
        /// Inicialize new JavascriptExpression
        /// </summary>
        /// <param name="expr">A lambda expression.</param>
        public JavascriptExpression(Expression expr)
        {
            if (!(expr is LambdaExpression)) throw new ArgumentOutOfRangeException("expr", "Must be of type LambdaExpression");
            _expr = (LambdaExpression)expr;
        }
        /// <summary>
        /// Convert LambdaExpression to Javascript expression
        /// </summary>
        /// <param name="expr">LambdaExpression to convert</param>
        /// <returns>A JavascriptExpression</returns>
        public static implicit operator JavascriptExpression(LambdaExpression expr)
        {
            return new JavascriptExpression(expr);
        }
        /// <summary>
        /// Converts the original expression to javascript function
        /// </summary>
        /// <returns>Javascript function</returns>
        public string Compile()
        {
            if(IsInline)
            {
                if (_expr.Parameters.Count != 1)
                throw new ArgumentException("Inline expression must have only one argument");
                if ((_expr.Body is MemberExpression))
                    _simpleMember = true;
                if ((_expr.Body is UnaryExpression))
                {
                    if(_expr.Body.NodeType==ExpressionType.Convert)
                    {
                        //conversions are not supported
                        if ((_expr.Body as UnaryExpression).Operand is MemberExpression)
                            _simpleMember = true;
                    }

                }
                //    throw new ArgumentException("This expression is not valid inline function");
                return ParseExpression(_expr.Body);
            }
            StringBuilder func = new StringBuilder();
            func.Append("function (");
            foreach (var p in _expr.Parameters)
            {
                func.Append(p.Name);
            }
            func.Append("){");
            func.Append("return (");            
            func.Append(ParseExpression(_expr.Body));
            func.Append(");}");
            return func.ToString();
        }

        public override string ToString()
        {
            return Compile();
        }
        private string ParseExpression(Expression expr)
        {
            if (expr is ParameterExpression)
            {
                if (IsInline) return null;
                return (expr as ParameterExpression).Name;
            }
            if (expr is IndexExpression)
            {
                IndexExpression ix = expr as IndexExpression;
                return ParseExpression(ix.Object) + "[" + string.Join(",", ix.Arguments.Select(x => ParseExpression(x))) + "]";
            }
            if (expr is MemberExpression)
            {
                MemberExpression ix = expr as MemberExpression;
                string prop = (this.MemberReplacement != null) ?this.MemberReplacement(ix.Member,_simpleMember):ix.Member.Name;
                _simpleMember = false;
                string o = ParseExpression(ix.Expression);
                if (o == null)
                {                    
                    if(IsInline&&ix.Expression is ParameterExpression)
                    {
                        return prop;
                    }
                    object val = GetExpressionValue(ix.Expression);
                    Type valType = val.GetType();
                    if (valType.GetCustomAttributes(typeof(JavascriptAttribute), false).Cast<JavascriptAttribute>()
                        .Any(x => x.ContextType == JavascriptContextType.This))
                    {
                        return "this." + prop;
                    }
                    var attr = ix.Member.GetCustomAttributes(typeof(JavascriptAttribute), false);
                    if (attr != null && attr.Length > 0)
                    {
                        if (attr.Cast<JavascriptAttribute>().Any(x => x.ContextType == JavascriptContextType.OnThis))
                        {
                            return "this." + prop;
                        }
                        return ix.Member.Name;
                    }
                    object valO = GetValue(ix.Member, val);
                    return JsonOrSimple(valO, false);

                    throw new Exception("Unable to get object property value");
                }
                else
                    return ParseExpression(ix.Expression) + "." + prop;
            }
            if (expr is NewExpression)
            {
                NewExpression ix = expr as NewExpression;
                if (ix.Members.Count == ix.Arguments.Count)
                {
                    return "{" + string.Join(",", ix.Members.JoinValues(ix.Arguments, (x, y) => x.Name + ":" + ParseExpression(y))) + "}";
                }
                else
                {
                    return "new " + ix.Type.Name + "(" + string.Join(",", ix.Arguments.Select(x => ParseExpression(x))) + ")";
                }
            }
            if (expr is MemberInitExpression)
            {
                MemberInitExpression ix = expr as MemberInitExpression;
                string si = "";
                if (ix.NewExpression.Arguments.Count > 0) si = string.Join(",", ix.NewExpression.Members.JoinValues(ix.NewExpression.Arguments, (x, y) => x.Name + ":" + ParseExpression(y)));
                if (ix.Bindings.Count == 0) return "{" + si + "}";
                return "{" + string.Join(",", ix.Bindings.Select(x => ((MemberAssignment)x).Member.Name + ":" + ParseExpression(((MemberAssignment)x).Expression))) + ((si == "") ? "" : "," + si) + "}";
            }
            if (expr is NewArrayExpression)
            {
                NewArrayExpression ix = expr as NewArrayExpression;
                return "[" + string.Join(",", ix.Expressions.Select(x => ParseExpression(x))) + "]";
            }
            if (expr is ConstantExpression)
                return this.JsonOrSimple((expr as ConstantExpression).Value);
            if (expr is BinaryExpression)
            {
                BinaryExpression bin = expr as BinaryExpression;
                switch (bin.NodeType)
                {
                    case ExpressionType.Add:
                        {
                            return ParseExpression(bin.Left) + "+" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.AddAssign:
                        {
                            return ParseExpression(bin.Left) + "+=" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.And:
                        {
                            return ParseExpression(bin.Left) + "&" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.AndAlso:
                        {
                            return ParseExpression(bin.Left) + "&&" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.AndAssign:
                        {
                            return ParseExpression(bin.Left) + "&=" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.Assign:
                        {
                            return ParseExpression(bin.Left) + "=" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.Coalesce:
                        {
                            return "(!" + ParseExpression(bin.Left) + ")?" + ParseExpression(bin.Right) + ":" + ParseExpression(bin.Left);
                        }
                    case ExpressionType.Divide:
                        {
                            return ParseExpression(bin.Left) + "/" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.DivideAssign:
                        {
                            return ParseExpression(bin.Left) + "/=" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.Equal:
                        {
                            return ParseExpression(bin.Left) + "==" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.ExclusiveOr:
                        {
                            return ParseExpression(bin.Left) + "^" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.ExclusiveOrAssign:
                        {
                            return ParseExpression(bin.Left) + "^=" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.GreaterThan:
                        {
                            return ParseExpression(bin.Left) + ">" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.GreaterThanOrEqual:
                        {
                            return ParseExpression(bin.Left) + ">=" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.LessThan:
                        {
                            return ParseExpression(bin.Left) + "<" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.LessThanOrEqual:
                        {
                            return ParseExpression(bin.Left) + "<=" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.Modulo:
                        {
                            return ParseExpression(bin.Left) + "%" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.ModuloAssign:
                        {
                            return ParseExpression(bin.Left) + "%=" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.Multiply:
                        {
                            return ParseExpression(bin.Left) + "*" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.MultiplyAssign:
                        {
                            return ParseExpression(bin.Left) + "*=" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.NotEqual:
                        {
                            return ParseExpression(bin.Left) + "!=" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.Or:
                        {
                            return ParseExpression(bin.Left) + "|" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.OrAssign:
                        {
                            return ParseExpression(bin.Left) + "|=" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.OrElse:
                        {
                            return ParseExpression(bin.Left) + "||" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.Power:
                        {
                            return ParseExpression(bin.Left) + "^" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.PowerAssign:
                        {
                            return ParseExpression(bin.Left) + "^=" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.Subtract:
                        {
                            return ParseExpression(bin.Left) + "-" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.SubtractAssign:
                        {
                            return ParseExpression(bin.Left) + "-=" + ParseExpression(bin.Right);
                        }
                    case ExpressionType.ArrayIndex:
                        return ParseExpression(bin.Left) + "[" + ParseExpression(bin.Right) + "]";
                    default: throw new Exception("Expression type " + bin.NodeType + " is not supported");
                }
            }
            if (expr is UnaryExpression)
            {
                UnaryExpression ue = expr as UnaryExpression;
                switch (ue.NodeType)
                {
                    case ExpressionType.ArrayLength:
                        return ParseExpression(ue.Operand) + ".length";
                    case ExpressionType.PreIncrementAssign:
                        return "++" + ParseExpression(ue.Operand);
                    case ExpressionType.PreDecrementAssign:
                        return "--" + ParseExpression(ue.Operand);
                    case ExpressionType.PostIncrementAssign:
                        return ParseExpression(ue.Operand) + "++";
                    case ExpressionType.PostDecrementAssign:
                        return ParseExpression(ue.Operand) + "--";
                    case ExpressionType.Convert:
                        {
                            if (ue.Operand is ConstantExpression)
                            {
                                object val = GetExpressionValue(ue.Operand);
                                if (Forcoft.Javascript.Helpers.TypeHelper.IsSimpleType(ue.Type))
                                {
                                    return this.JsonOrSimple(Convert.ChangeType(val, ue.Type), false);
                                }
                                else
                                {
                                    throw new Exception("Expression converting can be only evaluated on simple types");
                                }
                            }
                            else
                            {
                                //js conversions
                                return ParseExpression(ue.Operand);
                                //throw new Exception("Expression converting is not supported");
                            }
                        }
                    default: throw new Exception("Expression type " + ue.NodeType + " is not supported");
                }

            }
            if (expr is MethodCallExpression)
            {
                MethodCallExpression mc = expr as MethodCallExpression;
                object val = GetExpressionValue(mc.Object);
                Type valType = val.GetType();
                if (valType.GetCustomAttributes(typeof(JavascriptAttribute), false).Cast<JavascriptAttribute>()
                    .Any(x => x.ContextType == JavascriptContextType.This))
                {
                    return "this." + mc.Method.Name + "(" + string.Join(",", mc.Arguments.Select(x => ParseExpression(x))) + ")";
                }
                var attr = mc.Method.GetCustomAttributes(typeof(JavascriptAttribute), false);
                if (attr != null && attr.Length > 0)
                {
                    if (attr.Cast<JavascriptAttribute>().Any(x => x.ContextType == JavascriptContextType.OnThis))
                    {
                        return "this." + mc.Method.Name + "(" + string.Join(",", mc.Arguments.Select(x => ParseExpression(x))) + ")";
                    }
                    return mc.Method.Name + "(" + string.Join(",", mc.Arguments.Select(x => ParseExpression(x))) + ")";
                }
                string js = ParseExpression(mc.Object);
                if (js == null)
                {
                    throw new NotSupportedException("Runtime functions calling is not supported in this version");
                }
                else
                {
                    return "(" + js + ")." + mc.Method.Name + "(" + string.Join(",", mc.Arguments.Select(x => ParseExpression(x))) + ")";
                }
            }
            if (expr is ConditionalExpression)
            {
                ConditionalExpression conexp = expr as ConditionalExpression;
                return "((" + ParseExpression(conexp.Test) + ")?(" + ParseExpression(conexp.IfTrue) + ")" +
                    ((conexp.IfFalse != null) ? ":(" + ParseExpression(conexp.IfFalse) + "))" : ")");
            }
            throw new InvalidOperationException("Expression is not supported");
        }

        private object GetValue(System.Reflection.MemberInfo memberInfo, object val)
        {
            if (memberInfo is System.Reflection.FieldInfo)
            {
                return (memberInfo as System.Reflection.FieldInfo).GetValue(val);
            }
            if (memberInfo is System.Reflection.PropertyInfo)
            {
                var pi = (memberInfo as System.Reflection.PropertyInfo);
                if (!pi.CanRead)
                    throw new Exception("Property cannot be read");
                return pi.GetValue(val,null);
            }
            return new Exception("Cannot get value on type which in neither PropertyInfo nor FieldInfo");
        }
        object GetExpressionValue(Expression expression)
        {
            if (expression is ConstantExpression)
                return (expression as ConstantExpression).Value;
            throw new InvalidOperationException("Cannot get expression value on other expression type");
        }

        private string JsonOrSimple(object p,bool mightBeComplex=true)
        {
            Type t = p.GetType();
            if (t == typeof(string))
                return "\"" + p + "\"";
            if (t == typeof(char))
                return "\" -\"".Replace('-',(char)p);
            if(t==typeof(DateTime))
                return ((DateTime)p).ToBinary().ToString();
            if(t==typeof(TimeSpan))
                return ((TimeSpan)p).TotalMilliseconds.ToString();
            if (t.IsPrimitive)
                return p.ToString();
            if(t.IsArray)
            {
                return "["+string.Join(",",(( p as IEnumerable<object>).Select(x=>JsonOrSimple(x)))) +"]";
            }
            if(mightBeComplex)return null;
            return "{" + string.Join(",", t.GetProperties(System.Reflection.BindingFlags.GetProperty).Select(x => x.Name + ":" + JsonOrSimple(x.GetValue(p,null)))
                .Concat(t.GetFields(System.Reflection.BindingFlags.GetField).Select(x => x.Name + ":" + JsonOrSimple(x.GetValue(p))))) + "}";
        }
    }
}
