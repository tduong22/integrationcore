using System.Threading.Tasks;

namespace Integration.Common.Interface
{
    public interface IActivatableAction
    {
        Task OnActivateAsync();
        Task OnDeactivateAsync();
    }
}
