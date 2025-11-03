using System.Collections.Generic;
using UnityEngine;

public class PointsFromDestructionRule : MonoBehaviour, IDestructionListener
{
    [Header("Refs")]
    [SerializeField] 
    private MonoBehaviour boardRef;   
    [SerializeField] 
    private MonoBehaviour pointsRef;  

    [Header("Scoring")]
    // 젬 하나 기본점
    [SerializeField] 
    private int basePerGem = 20;
    // 잭 인접 1개당 보너스
    [SerializeField]
    private int bonusPerJackAdj = 300;

    private IBoardReadonly board;
    private IPoints points;

    private void Awake()
    {
        board = boardRef as IBoardReadonly;
        points = pointsRef as IPoints;
        if (board == null) Debug.LogError("[PointsFromDestructionRule] boardRef must implement IBoardReadonly.");
        if (points == null) Debug.LogError("[PointsFromDestructionRule] pointsRef must implement IPoints.");
    }

    public void OnGemsDestroyed(IReadOnlyCollection<Vector3Int> destroyed)
    {
        if (board == null || points == null || destroyed == null || destroyed.Count == 0) return;

        int total = 0;

        // 기본 점수: 젬 수 × 20
        total += destroyed.Count * basePerGem;

        // 보너스: 각 젬이 인접한 잭 개수 × 300
        foreach (var cell in destroyed)
        {
            int adjJacks = 0;
            foreach (var n in board.Neighbor6(cell))     
                if (board.IsBlocked(n)) adjJacks++;

            if (adjJacks > 0)
                total += adjJacks * bonusPerJackAdj;
        }

        if (total != 0)
            points.Add(total);
    }
}
