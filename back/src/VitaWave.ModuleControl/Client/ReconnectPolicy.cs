using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitaWave.ModuleControl.Client
{
    internal class ReconnectPolicy : IRetryPolicy
    {
        const int SECONDS_BEOFRE_NEXT_RETRY = 2;
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            return TimeSpan.FromSeconds(SECONDS_BEOFRE_NEXT_RETRY);
        }
    }
}
