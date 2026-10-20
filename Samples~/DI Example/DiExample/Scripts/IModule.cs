using System.Threading.Tasks;

namespace DiExample
{
    public interface IModule
    {
        Task OnEnterAsync();
        Task OnExitAsync();
    }

    public sealed class NullModule : IModule
    {
        Task IModule.OnEnterAsync()
        {
            return Task.CompletedTask;
        }

        Task IModule.OnExitAsync()
        {
            return Task.CompletedTask;
        }
    }
}