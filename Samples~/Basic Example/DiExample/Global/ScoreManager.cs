namespace DiExample
{
    public interface IScoreManager
    {
        int Score { get; }
        void IncreaseScore();
    }

    public class ScoreManager : IScoreManager
    {
        public int Score { get; private set; }

        public ScoreManager()
        {
            Score = 0;
        }

        public void IncreaseScore() => Score++;
    }
}