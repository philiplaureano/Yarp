namespace Yarp.Messages
{
    public class RequestVoteResult
    {
        public RequestVoteResult(int term, bool voteGranted)
        {
            Term = term;
            VoteGranted = voteGranted;
        }

        public int Term { get; }
        public bool VoteGranted { get; }
    }
}