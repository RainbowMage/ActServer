using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainbowMage.MiniParseServer
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
                .Replace("\"", "\\\"")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
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
