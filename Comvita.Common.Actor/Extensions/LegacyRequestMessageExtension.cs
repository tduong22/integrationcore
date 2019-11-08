using System;
using System.Net.Http;
using System.Text;
using Comvita.Common.Actor.Infrastructures.Services.Requests;

namespace Comvita.Common.Actor.Extensions
{
    public static class LegacyRequestMessageExtension
    {
        public static HttpRequestMessage ToHttpRequestMessage(this LegacyRequestMessage legacyRequestMessage)
        {
            var req = new HttpRequestMessage()
            {
                RequestUri = new Uri(legacyRequestMessage.RequestUri),
                Method = new HttpMethod(legacyRequestMessage.Method),
                Content = new StringContent(legacyRequestMessage.Content, Encoding.UTF8, "text/xml")
            };

            // append headers
            if (legacyRequestMessage.Headers != null)
            {
                req.Headers.Clear();
                foreach (var key in legacyRequestMessage.Headers.Keys)
                {
                    req.Headers.Add(key, legacyRequestMessage.Headers[key]);
                }
            }
            return req;
        }
    }
}