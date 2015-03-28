using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RainbowMage.ActServer
{
    public interface IExtension : IDisposable
    {
        /// <summary>
        /// Get an unique extension name (e.g. "SampleExtension"). 
        /// This is used as data type name.
        /// </summary>
        string ExtensionName { get; }

        /// <summary>
        /// User friendly name (e.g. "Sample Extension")
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Description of the extension (e.g. "Provides information about foo bar baz.").
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Process the requests.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="token"></param>
        void ProcessRequest(HttpListenerContext context, CancellationToken token);
    }
}
