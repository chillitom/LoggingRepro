using System.Threading.Tasks;
using Orleans;

namespace Interfaces {
    public interface IPingGrain : IGrainWithStringKey {
        Task<string> PingOrleans();
    }
}