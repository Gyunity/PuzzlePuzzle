using TMPro;
using UnityEngine;

public class HudUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private JackScoreService score;    // IScore
    [SerializeField] private MoveCountService moveCount;  // IAttempts

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI scoreLabel;
    [SerializeField] private TextMeshProUGUI movesLabel;


    private System.Action<int> _onScoreChanged;
    private System.Action _onScoreCleared;
    private System.Action<int> _onMovesChanged;
    private System.Action _onMovesExhausted;

    private void Awake()
    {
        _onScoreChanged = v => { if (scoreLabel) scoreLabel.text = v.ToString(); };
        _onScoreCleared = () => { if (scoreLabel) scoreLabel.text = 0.ToString(); };
        _onMovesChanged = v => { if (movesLabel) movesLabel.text = v.ToString(); };
        _onMovesExhausted = () => { if (movesLabel) movesLabel.text = 0.ToString(); };
    }

    private void OnEnable()
    {
        if (score)
        {
            score.Changed += _onScoreChanged;
            score.Cleared += _onScoreCleared;
            _onScoreChanged(score.Current); // 초기 동기화
        }
        if (moveCount)
        {
            moveCount.Changed += _onMovesChanged;
            moveCount.Done += _onMovesExhausted;
            _onMovesChanged(moveCount.Current); // 초기 동기화
        }
    }

    private void OnDisable()
    {
        if (score)
        {
            score.Changed -= _onScoreChanged;
            score.Cleared -= _onScoreCleared;
        }
        if (moveCount)
        {
            moveCount.Changed -= _onMovesChanged;
            moveCount.Done -= _onMovesExhausted;
        }
    }
}
