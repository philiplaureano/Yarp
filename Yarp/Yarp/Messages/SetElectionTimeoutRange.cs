namespace Yarp.Messages
{
    public class SetElectionTimeoutRange
    {
        public SetElectionTimeoutRange(int minMilliseconds, int maxMilliseconds)
        {
            MinMilliseconds = minMilliseconds;
            MaxMilliseconds = maxMilliseconds;
        }

        public int MinMilliseconds { get; }
        public int MaxMilliseconds { get; }
    }
}