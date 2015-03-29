using System;
using System.Linq;
using System.Net;
using System.Threading;
using RainbowMage.ActServer.Reflection.Helper;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using System.Reflection;

namespace RainbowMage.ActServer
{
    /// <summary>
    /// Wrapper class for IExtension interface implemented objects.
    /// </summary>
    public class ExtensionWrapper : IExtension
    {
        static readonly string ExtensionNamePropertyName;
        static readonly string DisplayNamePropertyName;
        static readonly string DescriptionPropertyName;
        static readonly string ProcessRequestMethodName;
        static readonly string DisposeMethodName;

        static ExtensionWrapper()
        {
#pragma warning disable 1720
            ExtensionNamePropertyName = Property.Of<string>(() => default(IExtension).ExtensionName).Name;
            DisplayNamePropertyName = Property.Of<string>(() => default(IExtension).DisplayName).Name;
            DescriptionPropertyName = Property.Of<string>(() => default(IExtension).Description).Name;
            ProcessRequestMethodName = Method.Of(() => default(IExtension).ProcessRequest(default(HttpListenerContext), default(CancellationToken))).Name;
            DisposeMethodName = Method.Of(() => default(IExtension).Dispose()).Name;
#pragma warning restore 1720
        }

        private object obj;

        public ExtensionWrapper(object extensionObject)
        {
            this.obj = extensionObject;
        }

        #region IExtension
        public string ExtensionName
        {
            get
            {
                return obj.GetProperty<string>(ExtensionNamePropertyName);
            }
        }

        public string DisplayName
        {
            get
            {
                return obj.GetProperty<string>(DisplayNamePropertyName);
            }
        }

        public string Description
        {
            get
            {
                return obj.GetProperty<string>(DescriptionPropertyName);
            }
        }

        public void ProcessRequest(HttpListenerContext context, CancellationToken token)
        {
            obj.InvokeMethod(
                ProcessRequestMethodName, 
                new Type[] { typeof(HttpListenerContext), typeof(CancellationToken) },
                new object[] { context, token });
        }

        public void Dispose()
        {
            obj.InvokeMethod(DisposeMethodName, new Type[0], new object[0]);
        }
        #endregion
    }
}
