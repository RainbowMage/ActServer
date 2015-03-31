using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RainbowMage.ActServer
{
    class AssemblyResolver : IDisposable
    {
        static readonly Regex assemblyNameParser = new Regex(
            @"(?<name>.+?), Version=(?<version>.+?), Culture=(?<culture>.+?), PublicKeyToken=(?<pubkey>.+)",
            RegexOptions.Compiled);

        public IDictionary<string, bool> Directories { get; set; }

        public AssemblyResolver(IDictionary<string, bool> directories)
        {
            this.Directories = new Dictionary<string, bool>();
            if (directories != null)
            {
                this.Directories = directories;
            }
            AppDomain.CurrentDomain.AssemblyResolve += CustomAssemblyResolve;
        }

        public AssemblyResolver()
            : this(null)
        {

        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= CustomAssemblyResolve;
        }

        private Assembly CustomAssemblyResolve(object sender, ResolveEventArgs e)
        {
            // Directories プロパティで指定されたディレクトリを基準にアセンブリを検索する
            foreach (var directory in this.Directories)
            {
                var searchDirectories = new List<string>();
                searchDirectories.Add(directory.Key);
                if (directory.Value)
                {
                    searchDirectories.AddRange(Directory.GetDirectories(directory.Key, "*", SearchOption.AllDirectories));
                }
                foreach (var searchDirectory in searchDirectories)
                {
                    var asmPath = "";
                    var match = assemblyNameParser.Match(e.Name);
                    if (match.Success)
                    {
                        var asmFileName = match.Groups["name"].Value + ".dll";
                        if (match.Groups["culture"].Value == "neutral")
                        {
                            asmPath = Path.Combine(searchDirectory, asmFileName);
                        }
                        else
                        {
                            asmPath = Path.Combine(searchDirectory, match.Groups["culture"].Value, asmFileName);
                        }
                    }
                    else
                    {
                        asmPath = Path.Combine(searchDirectory, e.Name + ".dll");
                    }

                    if (File.Exists(asmPath))
                    {
                        var asm = Assembly.LoadFile(asmPath);
                        OnAssemblyLoaded(asm);
                        return asm;
                    }
                }
            }

            return null;
        }

        protected void OnAssemblyLoaded(Assembly assembly)
        {
            if (this.AssemblyLoaded != null)
            {
                this.AssemblyLoaded(this, new AssemblyLoadEventArgs(assembly));
            }
        }

        public event EventHandler<AssemblyLoadEventArgs> AssemblyLoaded;
    }
}

