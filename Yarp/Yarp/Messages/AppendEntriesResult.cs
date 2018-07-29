namespace Yarp.Messages
{
    public class AppendEntriesResult
    {
        public AppendEntriesResult(int term, bool success)
        {
            Term = term;
            Success = success;
        }

        public int Term { get; }
        public bool Success { get; }
    }
}