using Comvita.Common.Actor.Interfaces;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Comvita.Common.Actor.Models
{
    public class ComvitaDomain
    {
        private string _id;
        private string _domain;
        private string _currencyCode;
        private string _entityCode;
        private string _paymentOwnGLCode;
        private string _qADUser;
        private string _paymentOwnBankNumber;
        private string _dateTimeFormat;
        private string _type = "ComvitaDomain";
        private List<ComvitaSite> _sites;

        [JsonProperty(PropertyName = "id")]
        public string Id { get => _id; set => _id = this.Domain; }

        [JsonProperty(PropertyName = "domain")]
        public string Domain {
            get => _domain;
            set {
                _id = value;
                _domain = value;
            }
        }

        [JsonProperty(PropertyName = "currencyCode")]
        public string CurrencyCode { get => _currencyCode; set => _currencyCode = value; }

        [JsonProperty(PropertyName = "entityCode")]
        public string EntityCode { get => _entityCode; set => _entityCode = value; }

        [JsonProperty(PropertyName = "paymentOwnGLCode")]
        public string PaymentOwnGLCode { get => _paymentOwnGLCode; set => _paymentOwnGLCode = value; }

        [JsonProperty(PropertyName = "qadUser")]
        public string QADUser { get => _qADUser; set => _qADUser = value; }

        [JsonProperty(PropertyName = "paymentOwnBankNumber")]
        public string PaymentOwnBankNumber { get => _paymentOwnBankNumber; set => _paymentOwnBankNumber = value; }

        [JsonProperty(PropertyName = "dateTimeFormat")]
        public string DateTimeFormat { get => _dateTimeFormat; set => _dateTimeFormat = value; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get => _type; set => _type = value; }

        [JsonProperty(PropertyName = "sites")]
        public List<ComvitaSite> Sites { get => _sites; }

        public ComvitaDomain()
        {
            this._sites = new List<ComvitaSite>();
        }
    }

    public class ComvitaSite : IPartitionable
    {
        private string _id;
        private string _domain;
        private string _site;
        private string _type = "ComvitaSite";
        private SiteConfiguration _siteConfiguration;

        [JsonProperty(PropertyName = "id")]
        public string Id { get => _id; set => _id = this.Domain; }

        [JsonProperty(PropertyName = "domain")]
        public string Domain
        {
            get => _domain;
            set => _domain = value;
        }

        [JsonProperty(PropertyName = "site")]
        public string Site { get => _site; set
            {
                _id = value;
                _site = value;
            }
        }

        [JsonProperty(PropertyName = "type")]
        public string Type { get => _type; set => _type = value; }

        [JsonProperty(PropertyName = "siteConfiguration")]
        public SiteConfiguration SiteConfiguration { get => _siteConfiguration; set => _siteConfiguration = value; }

        public string ExtractPartitionKey()
        {
            return this.Domain;
        }
    }

    public class SiteConfiguration
    {
        [JsonProperty(PropertyName = "inventoryPollingFrequencyInMinutes")]
        public int InventoryPollingFrequencyInMinutes { get; set; } = 60;

        [JsonIgnore]
        public static SiteConfiguration DefaultConfig => new SiteConfiguration();
    }
}