using System;
using System.Net;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Orleans;
using Interfaces;
using System.Threading.Tasks;

namespace Grains {
    public class PingGrain : Grain, IPingGrain {
        public Task<string> PingOrleans() {
            return Task.FromResult("pong");
        }
    }
}