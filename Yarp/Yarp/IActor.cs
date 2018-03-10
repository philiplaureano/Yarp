using System.Threading.Tasks;

namespace Yarp
{
    public interface IActor
    {
        Task TellAsync(IContext context);
    }
}