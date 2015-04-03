using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.Owin;
using Owin;
using RainbowMage.ActServer.Nancy;

namespace RainbowMage.ActServer
{
    public class OwinStartup
    {
        public void Configuration(IAppBuilder app)
        {
            var configuration = new Configuration()
            {
                HostType = HostType.OwinSelfHost
            };
            var options = new NancyOptions()
            {
                Bootstrapper = new Bootstrapper(configuration)
            }; 

            app.UseNancy(options);
        }
    }
}
