using System.Threading.Tasks;

namespace Integration.Common.Interface
{
    public interface IBlobStorageService
    {
        Task CreateFileAsync(string filename, byte[] fileData);

        Task<string> ReadFileContentAsync(string filename);

        Task<T> ReadFileContentAsync<T>(string filename);

        Task DeleteFileAsync(string filename);
    }
}
