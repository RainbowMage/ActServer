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
        static readonly PropertyInfo ExtensionNamePropertyInfo;
        static readonly PropertyInfo DisplayNamePropertyInfo;
        static readonly PropertyInfo DescriptionPropertyInfo;
        static readonly MethodInfo ProcessRequestMethodInfo;
        static readonly MethodInfo DisposeMethodInfo;

        static ExtensionWrapper()
        {
#pragma warning disable 1720
            ExtensionNamePropertyInfo = Property.Of<string>(() => default(IExtension).ExtensionName);
            DisplayNamePropertyInfo = Property.Of<string>(() => default(IExtension).DisplayName);
            DescriptionPropertyInfo = Property.Of<string>(() => default(IExtension).Description);
            ProcessRequestMethodInfo = Method.Of(() => default(IExtension).ProcessRequest(default(HttpListenerContext), default(CancellationToken)));
            DisposeMethodInfo = Method.Of(() => default(IExtension).Dispose());
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
                return (string)ExtensionNamePropertyInfo.GetValue(obj);
            }
        }

        public string DisplayName
        {
            get
            {
                return (string)DisplayNamePropertyInfo.GetValue(obj);
            }
        }

        public string Description
        {
            get
            {
                return (string)DescriptionPropertyInfo.GetValue(obj);
            }
        }

        public void ProcessRequest(HttpListenerContext context, CancellationToken token)
        {
            ProcessRequestMethodInfo.Invoke(obj, new object[] { context, token });
        }

        public void Dispose()
        {
            DisposeMethodInfo.Invoke(obj, new object[0]);
        }
        #endregion
    }
}
