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

    static class ReflectionExtension
    {
        public static void SetField<T>(this object obj, string name, T value)
        {
            var fieldInfo = obj.GetType().GetField(name);
            if (fieldInfo != null && fieldInfo.FieldType == typeof(T))
            {
                fieldInfo.SetValue(obj, value);
            }
            else
            {
                throw new MissingFieldException();
            }
        }

        public static T GetField<T>(this object obj, string name)
        {
            var fieldInfo = obj.GetType().GetField(name);
            if (fieldInfo != null && fieldInfo.FieldType == typeof(T))
            {
                return (T)fieldInfo.GetValue(obj);
            }
            else
            {
                throw new MissingFieldException();
            }
        }

        public static void SetProperty<T>(this object obj, string name, T value)
        {
            var propertyInfo = obj.GetType().GetProperty(name);
            if (propertyInfo != null && propertyInfo.PropertyType == typeof(T))
            {
                propertyInfo.SetValue(obj, value);
            }
            else
            {
                throw new MissingMemberException();
            }
        }

        public static T GetProperty<T>(this object obj, string name)
        {
            var propertyInfo = obj.GetType().GetProperty(name);
            if (propertyInfo != null && propertyInfo.PropertyType == typeof(T))
            {
                return (T)propertyInfo.GetValue(obj);
            }
            else
            {
                throw new MissingMemberException();
            }
        }

        public static void InvokeMethod(this object obj, string name, Type[] argTypes, object[] args)
        {
            obj.InvokeMethod(name, new Type[0], argTypes, args);
        }

        public static void InvokeMethod(this object obj, string name, Type[] genericTypes, Type[] argTypes, object[] args)
        {
            var methodInfo = GetMethodInfo(obj.GetType(), typeof(void), name, genericTypes, argTypes);
            methodInfo.Invoke(obj, args);
        }

        public static T InvokeMethod<T>(this object obj, string name, Type[] argTypes, object[] args)
        {
            return obj.InvokeMethod<T>(name, new Type[0], argTypes, args);
        }

        public static T InvokeMethod<T>(this object obj, string name, Type[] genericTypes, Type[] argTypes, object[] args)
        {
            var methodInfo = GetMethodInfo(obj.GetType(), typeof(T), name, genericTypes, argTypes);
            return (T)methodInfo.Invoke(obj, args);
        }

        private static MethodInfo GetMethodInfo(Type type, Type returnType, string name, Type[] genericTypes, Type[] argTypes)
        {
            foreach (var methodInfo in type.GetMethods())
            {
                // Check name
                if (methodInfo.Name != name)
                {
                    continue;
                }

                if (methodInfo.ReturnType != returnType)
                {
                    continue;
                }

                // Check arguments
                if (methodInfo.IsGenericMethodDefinition
                    && !IsValidGenericTypesForMethod(genericTypes, methodInfo))
                {
                    continue;
                }

                if (methodInfo.IsGenericMethodDefinition)
                {
                    var constructedMethodInfo = methodInfo.MakeGenericMethod(genericTypes);

                    if (IsValidArgumentTypesForMethod(argTypes, constructedMethodInfo))
                    {
                        return constructedMethodInfo;
                    }
                }
                else
                {
                    if (IsValidArgumentTypesForMethod(argTypes, methodInfo))
                    {
                        return methodInfo;
                    }
                }

            }

            throw new MissingMethodException();
        }

        private static bool IsValidGenericTypesForMethod(Type[] genericTypes, MethodInfo methodInfo)
        {
            var methodParams = methodInfo.GetParameters();

            if (methodParams.Length != genericTypes.Length)
            {
                return false;
            }

            for (var i = 0; i < methodParams.Length; i++)
            {
                if (methodParams[i].ParameterType.IsGenericParameter)
                {
                    if (!genericTypes[i].IsSuitableForGenericDefinition(methodParams[i].ParameterType))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsSuitableForGenericDefinition(this Type type, Type definition)
        {
            var attr = definition.GenericParameterAttributes;

            // where new()
            if (attr.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
            {
                if (type.GetConstructor(new Type[] { }) == null)
                {
                    return false;
                }
            }
            // where struct
            if (attr.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
            {
                if (!type.IsValueType)
                {
                    return false;
                }
            }
            // where class
            if (attr.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
            {
                if (!type.IsClass)
                {
                    return false;
                }
            }

            // where Type1, Type2, ...
            foreach (var constraint in definition.GetGenericParameterConstraints())
            {
                if (!constraint.IsAssignableFrom(type))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidArgumentTypesForMethod(Type[] argTypes, MethodInfo methodInfo)
        {
            var methodParams = methodInfo.GetParameters();

            if (methodParams.Length != argTypes.Length)
            {
                return false;
            }

            for (var i = 0; i < methodParams.Length; i++)
            {
                if (!argTypes[i].IsAssignableFrom(methodParams[i].ParameterType))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
