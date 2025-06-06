using VitaWave.ModuleControl.Parsing.TLVs;

namespace VitaWave.ModuleControl.Interfaces
{
    public interface ISerialProcessor
    {

        public bool IsRunning { get; }
        public void AddToQueue(byte[] buffer, FrameHeader header);
        public void Run();
        public void Stop();
        public void Dispose();
    }
}
