using Microsoft.AspNetCore.SignalR.Client;
using VitaWave.Common;
using VitaWave.ModuleControl.Interfaces;

namespace AlgorithmTester
{
    public class DoNothingSignalRClient : ISignalRClient
    {
        HubConnectionState ISignalRClient.Status => HubConnectionState.Connected;

        Task ISignalRClient.SendDataAsync(object data)
        {
            var events = (List<ResultEvent>) data;
            
            foreach(var result in events)
            {
                Console.WriteLine(result.ResultId.ToString());
            }

            return Task.CompletedTask;
        }

        Task ISignalRClient.StartAsync()
        {
            return Task.CompletedTask;
        }
    }
}
