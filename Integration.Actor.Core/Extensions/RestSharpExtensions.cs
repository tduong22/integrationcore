using RestSharp;
using System;
using System.Net;
using System.Text;

namespace Integration.Common.Extensions
{
    public static class RestSharpExtensions
    {
        public static bool IsScuccessStatusCode(this HttpStatusCode responseCode)
        {
            var numericResponse = (int)responseCode;

            const int statusCodeOk = (int)HttpStatusCode.OK;

            const int statusCodeBadRequest = (int)HttpStatusCode.BadRequest;

            return numericResponse >= statusCodeOk &&
                   numericResponse < statusCodeBadRequest;
        }

        public static bool IsSuccessful(this IRestResponse response)
        {
            return response.StatusCode.IsScuccessStatusCode() &&
                   response.ResponseStatus == ResponseStatus.Completed;
        }

        public static void EnsureResponseWasSuccessful(this IRestClient client, IRestRequest request, IRestResponse response)
        {
            if (response.IsSuccessful())
            {
                return;
            }

            var requestUri = client.BuildUri(request);

            throw RestException.CreateException(requestUri, response);
        }
    }

    public class RestException : Exception
    {
        public RestException(HttpStatusCode httpStatusCode, Uri requestUri, string content, string message, Exception innerException)
          : base(message, innerException)
        {
            HttpStatusCode = httpStatusCode;
            RequestUri = requestUri;
            Content = content;
        }

        public HttpStatusCode HttpStatusCode { get; private set; }

        public Uri RequestUri { get; private set; }

        public string Content { get; private set; }

        public static RestException CreateException(Uri requestUri, IRestResponse response)
        {
            Exception innerException = null;

            var messageBuilder = new StringBuilder();

            messageBuilder.AppendLine(string.Format("Processing request [{0}] resulted with following errors:", requestUri));

            if (response.StatusCode.IsScuccessStatusCode() == false)
            {
                messageBuilder.AppendLine($"- Server responded with unsuccessfult status code: {response.StatusCode}. Response: {response.Content}");
                innerException = new Exception($"Server responded with unsuccessfult status code: {response.StatusCode}. Response: {response.Content}");
            }

            if (response.ErrorException != null)
            {
                messageBuilder.AppendLine("- An exception occurred while processing request: " + response.ErrorMessage);

                innerException = response.ErrorException;
            }

            return new RestException(response.StatusCode, requestUri, response.Content, messageBuilder.ToString(), innerException);
        }
    }
}