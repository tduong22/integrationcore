using Comvita.Common.Actor.Models;
using System.Collections.Generic;

namespace Comvita.Common.Actor.Interfaces
{
    public interface IQADSiteService
    {
        List<ComvitaDomain> GetAllDomains();
        ComvitaDomain GetDomain(string domain);
        List<ComvitaSite> GetAllSites();
        List<ComvitaSite> GetSiteByDomain(string domain);
    }
}
