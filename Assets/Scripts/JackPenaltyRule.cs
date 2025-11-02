using System.Collections.Generic;
using UnityEngine;

//잭 규칙
public class JackPenaltyRule : MonoBehaviour, IDestructionListener
{
    [SerializeField]
    private MonoBehaviour boardRef;
    [SerializeField]
    private MonoBehaviour scoreRef;

    private IBoardReadonly board;
    private IScore score;
    private void Awake()
    {
        board = boardRef as IBoardReadonly;
        score = scoreRef as IScore;
        if (board == null) Debug.LogError("[JackPenaltyRule] boardRef는 IBoardReadonly를 구현해야 합니다.");
        if (score == null) Debug.LogError("[JackPenaltyRule] scoreRef는 IScore를 구현해야 합니다.");
    }
    public void OnGemsDestroyed(IReadOnlyCollection<Vector3Int> destroyed)
    {
        if (board == null || score == null || destroyed == null || destroyed.Count == 0)
            return;
        // 이번 파괴 집합에 ‘영향을 받은 잭(장애물)’들을 모은다 (중복 제거)
        var touchedJacks = new HashSet<Vector3Int>();

        // destroyed 중 하나라도 '잭(=blocker)과 인접'이면 1점 차감 (횟수 기준)
        foreach (var cell in destroyed)
        {
            foreach (var n in board.Neighbor6(cell))
            {
                if (board.IsBlocked(n))
                {
                    // 같은 잭은 여러 셀이 터져도 1회만 카운트
                    touchedJacks.Add(n);
                }
            }
        }
        int penalty = touchedJacks.Count;   // 잭의 개수만큼 차감
        if (penalty > 0)
            score.Decrement(penalty);
    }
}
