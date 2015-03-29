using RainbowMage.ActServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExtensionTest
{
    public class MiniParseExtension : IExtension
    {
        #region IExtension
        public string ExtensionName
        {
            get { return "RainbowMage.ExtensionTest"; }
        }

        public string DisplayName
        {
            get { return "Extension Test"; }
        }

        public string Description
        {
            get { return "TEST."; }
        }

        public void ProcessRequest(HttpListenerContext context, CancellationToken token)
        {
            Server.SendJsonResponse(context, "{ \"IT_WORKS\": true }");
        }


        public void Dispose()
        {

        }
        #endregion
    }
}
