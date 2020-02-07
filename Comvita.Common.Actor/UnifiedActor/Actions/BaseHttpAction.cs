using Comvita.Common.Actor.Extensions;
using Integration.Common.Actor.UnifiedActor.Actions;
using Integration.Common.Utility.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.Actions
{
    public abstract class BaseHttpAction : BaseAction
    {
        protected IRestClient RestClient;
        protected IAuthenticator Authenticator;
        protected string BaseUrl;
        private IDictionary<string, string> certificateBlobConfig;
        private readonly IBlobStorageConfiguration _blobStorageConfiguration;
        private readonly IServiceConfiguration _serviceConfiguration;
        private byte[] certificateBuff;

        protected Encoding Encoding;

        public BaseHttpAction(
                             IBlobStorageConfiguration blobStorageConfiguration,
                             IServiceConfiguration serviceConfiguration,
                             IAuthenticator authenticator = null)
            : base()
        {
            Authenticator = authenticator;
            _blobStorageConfiguration = blobStorageConfiguration;
            _serviceConfiguration = serviceConfiguration;
        }

        protected virtual Task PreRequestAsync()
        {
            if (Authenticator != null)
            {
                RestClient.Authenticator = Authenticator;
            }

            return Task.CompletedTask;
        }

        protected virtual Task<IEnumerable<HttpHeader>> AddHeaders(IEnumerable<HttpHeader> headers = null)
        {
            return Task.FromResult(headers);
        }

        protected async Task<object> SendMessageAsync(string url, object model, Type typeOfResponse,
            IEnumerable<HttpHeader> headers = null, DataFormat dataFormat = DataFormat.Json, ISerializer jsonSerializer = null,
            bool includeCertificate = false, CancellationToken cancellationToken = default)
        {
            RestClient = await GetRestClientAsync(includeCertificate);

            var restRequest = await PrepareRequestAsync(url, model, Method.POST, headers, dataFormat, jsonSerializer, includeCertificate);
            Logger.LogInformation($"{CurrentActor} is going to dispatch http request {jsonSerializer.Serialize(model)}");
            var response = await RestClient.ExecuteTaskAsync(restRequest, cancellationToken);

            // ensure success response
            RestClient.EnsureResponseWasSuccessful(restRequest, response);

            Logger.LogInformation($"{CurrentActor} response from dispatch successfully with status code {response.StatusCode}, {response.StatusDescription} : {response.Content}");
            var result = HandleResponse(response, typeOfResponse);
            return result;
        }

        public virtual object HandleResponse(IRestResponse response, Type typeOfResponse)
        {
            var result = JsonConvert.DeserializeObject(response.Content,
                typeOfResponse,
                new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                        {
                            OverrideSpecifiedNames = false
                        }
                    }
                });
            return result;
        }

        protected async Task<TResponse> SendMessageAsync<TResponse>(string url, object model, IEnumerable<HttpHeader> headers = null, DataFormat dataFormat = DataFormat.Json, ISerializer jsonSerializer = null,
            bool includeCertificate = false, CancellationToken cancellationToken = default) where TResponse : new()
        {
            RestClient = await GetRestClientAsync(includeCertificate);

            var restRequest = await PrepareRequestAsync(url, model, Method.POST, headers, dataFormat, jsonSerializer);
            Logger.LogInformation($"{CurrentActor} is going to dispatch http request {restRequest}");
            var response = await RestClient.PostAsync<TResponse>(restRequest);
            Logger.LogInformation($"{CurrentActor} response from dispatch successfully");
            return response;
        }

        private async Task<RestClient> GetRestClientAsync(bool includeCert = false)
        {
            var client = new RestClient(BaseUrl);
            if (Encoding != null)
            {
                client.Encoding = Encoding;
            }
            if (includeCert)
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.DefaultConnectionLimit = 9999;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                await GetCertificateAsync();
                X509Certificate2 certificate = new X509Certificate2(certificateBuff);
                client.ClientCertificates = new X509CertificateCollection() { certificate };
                client.Proxy = new WebProxy();
            }

            return client;
        }

        private async Task GetCertificateAsync()
        {
            if (certificateBuff == null)
            {
                if (certificateBlobConfig == null)
                    certificateBlobConfig = _serviceConfiguration.GetConfigSection("CertBlobConfig");

                if (!certificateBlobConfig.ContainsKey("ConnectionString")) throw new Exception("Please add ConnectionString parameter to CertBlobConfig section");
                if (!certificateBlobConfig.ContainsKey("ContainerName")) throw new Exception("Please add ContainerName parameter to CertBlobConfig section");
                if (!certificateBlobConfig.ContainsKey("Filename")) throw new Exception("Please add Filename parameter to CertBlobConfig section");

                var connectionString = certificateBlobConfig["ConnectionString"];
                var containerName = certificateBlobConfig["ContainerName"];
                var filename = certificateBlobConfig["Filename"];

                var fileContent = await _blobStorageConfiguration.GetContentFromConfigurationFile(connectionString, containerName, filename);

                certificateBuff = Encoding.Unicode.GetBytes(fileContent);
            }
        }

        private async Task<RestRequest> PrepareRequestAsync(string url, object model = null, Method method = Method.POST,
            IEnumerable<HttpHeader> headers = null, DataFormat dataFormat = DataFormat.Json, ISerializer jsonSerializer = null,
            bool includeCertificate = false)
        {
            var restRequest = new RestRequest(url, method, dataFormat);

            await PreRequestAsync();
            if (method == Method.POST)
            {
                if (model != null)
                {
                    switch (dataFormat)
                    {
                        case DataFormat.Json:
                            restRequest.AddJsonBody(model);
                            restRequest.JsonSerializer = jsonSerializer;
                            break;

                        case DataFormat.Xml:
                            restRequest.AddXmlBody(model);
                            restRequest.XmlSerializer = new DotNetXmlSerializer();
                            break;

                        case DataFormat.None:
                            {
                                if (model is string body)
                                {
                                    restRequest.AddParameter("text/xml", body, ParameterType.RequestBody);
                                }

                                break;
                            }
                    }
                }
            }

            var requestHeaders = await AddHeaders(headers);

            foreach (var header in requestHeaders)
            {
                restRequest.AddHeader(header.Name, header.Value);
            }

            return restRequest;
        }

        protected async Task<TResponse> GetMessageAsync<TResponse>(string url, IEnumerable<HttpHeader> headers = null, DataFormat dataFormat = DataFormat.Json,
            bool includeCertificate = false, CancellationToken cancellationToken = default) where TResponse : new()
        {
            RestClient = await GetRestClientAsync(includeCertificate);
            var restRequest = await PrepareRequestAsync(url, null, method: Method.GET, headers, dataFormat);
            Logger.LogInformation($"{CurrentActor} is going to dispatch http request {restRequest}");
            var response = await RestClient.GetAsync<TResponse>(restRequest);
            Logger.LogInformation($"{CurrentActor} response from dispatch successfully");
            return response;
        }
    }
}
