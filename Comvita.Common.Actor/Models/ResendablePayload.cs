namespace Comvita.Common.Actor.Models
{
    public class ResendablePayload
    {
        public string ContentType { get; set; }

        public string Payload { get; set; }

        public string HttpMethod { get; set; }

        public ResendablePayload(string contentType, string payload, string httpMethod)
        {
            ContentType = contentType;
            Payload = payload;
            HttpMethod = httpMethod;
        }
    }
}
