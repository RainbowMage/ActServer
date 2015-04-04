using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainbowMage.ActServer
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    class RestartRequiredAttribute : Attribute
    {
        public string Message { get; set; }
    }
}
