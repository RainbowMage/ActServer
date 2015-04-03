using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.Diagnostics;
using Nancy.TinyIoc;

namespace RainbowMage.ActServer.Nancy
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        private IBootstrapParams bootParams;

        public Bootstrapper(IBootstrapParams bootParams)
        {
            this.bootParams = bootParams;
        }

        protected override IRootPathProvider RootPathProvider
        {
            get { return new RootPathProvider(bootParams.RootDirectory); }
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);

            nancyConventions.StaticContentsConventions.Clear();
            nancyConventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddDirectory("", "assets")
            );
            nancyConventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddDirectory("/", "/index.html")
            );
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            container.Register(bootParams);
        }
    }
}
