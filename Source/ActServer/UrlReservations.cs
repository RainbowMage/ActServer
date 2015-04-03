using System;
using System.Diagnostics;
using System.Security.Principal;

namespace RainbowMage.ActServer
{
    static class UrlReservations
    {
        // Nancy/src/Nancy.Hosting.Self/NetSh.cs

        public static bool TryAdd(string prefix)
        {
            return AddUrlAcl(prefix, GetEveryoneName());
        }

        private static string GetEveryoneName()
        {
            var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var account = (NTAccount)sid.Translate(typeof(NTAccount));

            return account != null
                ? account.Value
                : "Everyone";
        }

        private static bool AddUrlAcl(string url, string user)
        {
            try
            {
                var arguments = GetParameters(url, user);

                return RunElevated("netsh", arguments);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string GetParameters(string url, string user)
        {
            return string.Format("http add urlacl url={0} user={1}", url, user);
        }

        private static bool RunElevated(string file, string args)
        {
            var process = CreateProcess(args, file);

            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }

        private static Process CreateProcess(string args, string file)
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Verb = "runas",
                    Arguments = args,
                    FileName = file,
                }
            };
        }
    }
}
