using System.Threading.Tasks;

namespace DiExample
{
    public interface IModule
    {
        Task OnEnterAsync();
        Task OnExitAsync();
    }

    public class NullModule : IModule
    {
        public Task OnEnterAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnExitAsync()
        {
            return Task.CompletedTask;
        }
    }
}