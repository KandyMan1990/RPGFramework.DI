namespace DiExample
{
    public interface IScoreManager
    {
        int Score { get; }
        void IncreaseScore();
    }

    public class ScoreManager : IScoreManager
    {
        private int m_Score;

        int IScoreManager.Score => m_Score;

        void IScoreManager.IncreaseScore() => m_Score++;
    }
}