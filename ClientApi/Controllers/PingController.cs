using System.Threading.Tasks;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace ClientApi.Controllers {
    [ApiController]
    [Route("ping")]
    public class PingController : ControllerBase {
        private readonly IGrainFactory _factory;

        public PingController(IGrainFactory factory) {
            _factory = factory;
        }

        [HttpGet("silo")]
        [AllowAnonymous]
        public Task<string> GetSiloPing() {
            return _factory.GetGrain<IPingGrain>("ping").PingOrleans();
        }

        [HttpGet("webapi")]
        [AllowAnonymous]
        public Task<string> GetWebApiPing() {
            return Task.FromResult("pong");
        }
    }
}
