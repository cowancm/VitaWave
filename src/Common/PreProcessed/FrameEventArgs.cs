using ModuleControl.Parsing;

namespace ModuleControl.Communication
{
    public class FrameEventArgs : EventArgs
    {
        public Event? FrameEvent { get; }

        public FrameEventArgs(Event? frameEvent)
        {
            FrameEvent = frameEvent;
        }
    }
}
