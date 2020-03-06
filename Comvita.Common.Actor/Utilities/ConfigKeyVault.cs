using System;
using System.Threading.Tasks;
using Integration.Common.Utility.Interfaces;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Comvita.Common.Actor.Utilities
{
    public class ConfigKeyVault : IConfigKeyVault
    {
        private static string _clientSecret;
        private static string _clientId;
        private static string _keyVaultEndPoint;

        public ConfigKeyVault(string keyVaultEndpoint, string clientId, string clientSecret)
        {
            _keyVaultEndPoint = keyVaultEndpoint;
            _clientId = clientId;
            _clientSecret = clientSecret;
        }

        public static async Task<string> GetToken(string authority, string resource, string scope)
        {
            try
            {
                var authContext = new AuthenticationContext(authority);
                var clientCred = new ClientCredential(_clientId,
                    _clientSecret);
                var result = await authContext.AcquireTokenAsync(resource, clientCred).ConfigureAwait(false);

                if (result == null)
                    throw new InvalidOperationException("Failed to obtain the JWT token");

                return result.AccessToken;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
        public async Task<string> GetSecureSecret(string secretName)
        {
            var kv = new KeyVaultClient(GetToken);
            var sec = await kv.GetSecretAsync(_keyVaultEndPoint, secretName);
            return sec.Value;
        }
    }
}
