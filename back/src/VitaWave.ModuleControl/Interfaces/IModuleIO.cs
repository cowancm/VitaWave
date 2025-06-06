﻿using System.ComponentModel;

namespace VitaWave.ModuleControl.Interfaces
{
    public interface IModuleIO
    {
        event PropertyChangedEventHandler? PropertyChanged;

        State Status { get; }
        State InitializePorts();
        State Run();
        State Pause();
        State Stop();

        Task<bool> TryWriteConfigFromFile();
        Task<bool> TryWriteConfigFromFile(string[] configStrings);
    }

    public enum State
    {
        AwaitingPortInit,       //We haven't started anything yet
        Running,                //Actively waiting on events
        Paused                  //We can start whenever
    }
}
