using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Nancy;

namespace RainbowMage.ActServer.Nancy
{
    public class RootPathProvider : IRootPathProvider
    {
        private string rootPath;

        public RootPathProvider(string rootPath)
        {
            this.rootPath = rootPath;
        }

        public string GetRootPath()
        {
            return rootPath;
        }
    }
}
