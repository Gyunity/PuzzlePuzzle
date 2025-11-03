using UnityEngine;

public class GameOutcomeCoordinator : MonoBehaviour
{
    [SerializeField] 
    private MonoBehaviour scoreRef;
    [SerializeField] 
    private MonoBehaviour moveCountRef;
    [SerializeField] 
    private GameObject winPanel;
    [SerializeField] 
    private GameObject losePanel;

    private IScore score;
    private IMoveCount moveCount;
    private void Awake()
    {
        score = scoreRef as IScore;
        moveCount = moveCountRef as IMoveCount;
    }

    void OnEnable()
    {
        if (score != null)
        {
            score.Cleared += OnWin; 
            score.Changed += OnScoreChanged;
        }
        if (moveCount != null)
        {
            moveCount.Done += OnAttemptsExhausted;
        }
    }
    void OnDisable()
    {
        if (score != null)
        {
            score.Cleared -= OnWin;
            score.Changed -= OnScoreChanged;
        }
        if (moveCount != null)
        {
            moveCount.Done -= OnAttemptsExhausted;
        }
    }

    private void OnScoreChanged(int current)
    {
        if (current <= 0) OnWin();
    }

    private void OnAttemptsExhausted()
    {
        // 잭 점수 여전히 남아있으면 패배
        if (score != null && score.Current > 0) OnLose();
       
    }

    private void OnWin()
    {
        if (winPanel) winPanel.SetActive(true);
        if (losePanel) losePanel.SetActive(false);
       
    }

    private void OnLose()
    {
        if (losePanel) losePanel.SetActive(true);
        if (winPanel) winPanel.SetActive(false);
    }
}
