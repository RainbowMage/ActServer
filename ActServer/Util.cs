using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainbowMage.ActServer
{
    static class Util
    {
        /// <summary>
        /// JSON 用に文字列をエスケープします。
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string CreateJsonSafeString(string str)
        {
            return str
                .Replace("\"", "\\\"") // quotation mark
                .Replace("\\", "\\\\") // reverse solidus
                // escaping solidus are not required 
                .Replace("\b", "\\b")  // backspace
                .Replace("\f", "\\f")  // form feed
                .Replace("\n", "\\n")  // line feed
                .Replace("\r", "\\r")  // carriage return
                .Replace("\t", "\\t"); // tab
        }

        /// <summary>
        /// 文字列中に含まれる double.NaN.ToString() で出力される文字列を指定した文字列で置換します。
        /// </summary>
        /// <param name="str"></param>
        /// <param name="replace"></param>
        /// <returns></returns>
        public static string ReplaceNaNString(string str, string replace)
        {
            return str.Replace(double.NaN.ToString(), replace);
        }
    }
}
