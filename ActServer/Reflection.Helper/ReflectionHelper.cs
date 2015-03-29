using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RainbowMage.ActServer.Reflection.Helper
{
    static class Method
    {
        public static MethodInfo Of<T>(Expression<Func<T>> expr)
        {
            return ((MethodCallExpression)expr.Body).Method;
        }

        public static MethodInfo Of(Expression<Action> expr)
        {
            return ((MethodCallExpression)expr.Body).Method;
        }
    }

    static class Constructor
    {
        public static ConstructorInfo Of<T>(Expression<Func<T>> expr)
        {
            return ((NewExpression)expr.Body).Constructor;
        }
    }

    static class Property
    {
        public static PropertyInfo Of<T>(Expression<Func<T>> expr)
        {
            return (PropertyInfo)((MemberExpression)expr.Body).Member;
        }
    }

    static class Field
    {
        public static FieldInfo Of<T>(Expression<Func<T>> expr)
        {
            return (FieldInfo)((MemberExpression)expr.Body).Member;
        }
    }
}
