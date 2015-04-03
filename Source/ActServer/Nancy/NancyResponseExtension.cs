using System.Text;
using Nancy;

namespace RainbowMage.ActServer
{
    public static class NancyResponseHelper
    {
        public static Response AsJsonErrorMessage(this IResponseFormatter formatter, string message, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return formatter.AsJson(new { IsError = true, Message = message }, statusCode);
        }

        public static Response AsEmptyJson(this IResponseFormatter formatter, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return formatter.AsJson(new { }, statusCode);
        }

        public static Response AsJson(this IResponseFormatter formatter, string jsonString, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var data = Encoding.UTF8.GetBytes(jsonString);
            var response = new Response
            {
                ContentType = "application/json",
                Contents = stream => stream.Write(data, 0, data.Length)
            };

            return response;
        }
    }
}
