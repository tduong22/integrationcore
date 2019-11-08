using Comvita.Common.Actor.Constants;
using Comvita.Common.Actor.Interfaces;
using Comvita.Common.Actor.Models;
using Newtonsoft.Json;
using RestSharp;
using System.Collections.Generic;

namespace Comvita.Common.Actor.Infrastructures.Services
{
    public class QADSiteService : IQADSiteService
    {
        public List<ComvitaDomain> GetAllDomains()
        {
            var client = new RestClient($"{CommonConstants.COMVITA_SITE_QUERY_SERVICE_URL}/api/comvitadomains");
            var request = new RestRequest(Method.GET);
            var response = client.Execute(request);
            var domains = JsonConvert.DeserializeObject<List<ComvitaDomain>>(response.Content);
            return domains;
        }

        public List<ComvitaSite> GetAllSites()
        {
            var client = new RestClient($"{CommonConstants.COMVITA_SITE_QUERY_SERVICE_URL}/api/comvitasites");
            var request = new RestRequest(Method.GET);
            var response = client.Execute(request);
            var sites = JsonConvert.DeserializeObject<List<ComvitaSite>>(response.Content);
            return sites;
        }

        public ComvitaDomain GetDomain(string domain)
        {
            var client = new RestClient($"{CommonConstants.COMVITA_SITE_QUERY_SERVICE_URL}/api/comvitadomains/{domain}");
            var request = new RestRequest(Method.GET);
            var response = client.Execute(request);
            var domainRes = JsonConvert.DeserializeObject<ComvitaDomain>(response.Content);
            return domainRes;
        }

        public List<ComvitaSite> GetSiteByDomain(string domain)
        {
            var client = new RestClient($"{CommonConstants.COMVITA_SITE_QUERY_SERVICE_URL}/api/comvitasites/{domain}");
            var request = new RestRequest(Method.GET);
            var response = client.Execute(request);
            var sites = JsonConvert.DeserializeObject<List<ComvitaSite>>(response.Content);
            return sites;
        }
    }
}