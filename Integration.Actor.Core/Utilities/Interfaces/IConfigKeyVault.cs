using System.Threading.Tasks;

namespace Integration.Common.Utility.Interfaces
{
    public interface IConfigKeyVault
    {
        Task<string> GetSecureSecret(string secretName);
    }
}
