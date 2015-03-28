using System;
using System.Linq;
using System.Net;
using System.Threading;

namespace RainbowMage.ActServer
{
    /// <summary>
    /// Wrapper class for IExtension interface implemented objects.
    /// </summary>
    public class ExtensionWrapper : IExtension
    {
        private object extensionObject;

        public ExtensionWrapper(object extensionObject)
        {
            this.extensionObject = extensionObject;
        }

        #region IExtension
        public string ExtensionName
        {
            get
            {
                return GetPropertyValue<string>(extensionObject, "ExtensionName");
            }
        }

        public string DisplayName
        {
            get
            {
                return GetPropertyValue<string>(extensionObject, "DisplayName");
            }
        }

        public string Description
        {
            get
            {
                return GetPropertyValue<string>(extensionObject, "Description");
            }
        }

        public void ProcessRequest(HttpListenerContext context, CancellationToken token)
        {
            InvokeMethod(extensionObject, "ProcessRequest", context, token);
        }

        public void Dispose()
        {
            InvokeMethod(extensionObject, "Dispose");
        }
        #endregion

        private static T GetPropertyValue<T>(object obj, string propertyName)
        {
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop != null && prop.PropertyType == typeof(T))
            {
                return (T)prop.GetValue(obj);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void InvokeMethod(object obj, string methodName, params object[] args)
        {
            var argTypes = args.Select((x) => x.GetType()).ToArray();
            var method = obj.GetType().GetMethod(methodName, argTypes);
            if (method != null && method.ReturnType == typeof(void))
            {
                method.Invoke(obj, args);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static T InvokeMethod<T>(object obj, string methodName, params object[] args)
        {
            var argTypes = args.Select((x) => x.GetType()).ToArray();
            var method = obj.GetType().GetMethod(methodName, argTypes);
            if (method != null && method.ReturnType == typeof(T))
            {
                return (T)method.Invoke(obj, args);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
