using System.Collections.Generic;
using UnityEngine;

public class HapticsAndShakeListener : MonoBehaviour, IDestructionListener
{
    [SerializeField] 
    private MonoBehaviour boardRef; 
    [SerializeField] 
    private CameraShaker shaker;
    private IBoardReadonly board;

    void Awake()
    {
        board = boardRef as IBoardReadonly;
        if (!shaker) shaker = FindObjectOfType<CameraShaker>();
    }

    // destroyed: 이번 프레임에 파괴될 보드셀들(아직 Destroy 전)
    public void OnGemsDestroyed(IReadOnlyCollection<Vector3Int> destroyed)
    {
        if (destroyed == null || destroyed.Count == 0) return;

        bool containsSpecial = false;

        // 보드에서 '파괴 예정' 젬들을 미리 들여다봄
        foreach (var cell in destroyed)
        {
            if (board.TryPeekGem(cell, out var gem) && gem != null && gem.IsLineBlaster())
            {
                containsSpecial = true;
                break;
            }
        }

        if (containsSpecial)
        {
            VibrationManager.VibrateMillis(500);
            if (shaker) shaker.Shake(0.50f, 0.20f);
        }
        else
        {
            VibrationManager.VibrateMillis(100);
            if (shaker) shaker.Shake(0.10f, 0.10f);
        }
    }
}
