using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cw3.Middlewares
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        public LoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task InvokeAsync(HttpContext httpContext)
        {
            if (httpContext.Request != null)
            {
                httpContext.Request.EnableBuffering();

                string path = httpContext.Request.Path;
                string method = httpContext.Request.Method;
                string query = httpContext.Request.QueryString.ToString();
                string body = "";

                using (var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, true,1024,true))
                {
                    body = await reader.ReadToEndAsync();
                    httpContext.Request.Body.Seek(0, SeekOrigin.Begin);
                }

                using (StreamWriter writetext = File.AppendText(@".\requestsLog.txt"))
                {
                    writetext.WriteLine($"HTTP:{method};PATH:{path};BODY:{body};QUERYSTRING:{query}");
                }
            }

            if (_next != null)
            await _next(httpContext);
        }
    }
}
