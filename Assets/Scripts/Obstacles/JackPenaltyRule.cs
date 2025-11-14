using System.Collections.Generic;
using UnityEngine;

//잭 규칙
public class JackPenaltyRule : MonoBehaviour, IDestructionListener
{
    [SerializeField]
    private MonoBehaviour boardRef;
    [SerializeField]
    private MonoBehaviour scoreRef;
    [SerializeField]
    private JackPenaltyFlyVFX vfx;
    [SerializeField] private JackFactory jackFactory;

    [Header("Jack visuals (옵션)")]
    private IBoardReadonly board;
    private IScore score;

    // cell -> JackVisuals 매핑
    private readonly Dictionary<Vector3Int, JackVisuals> _jackByCell = new();
    private void Awake()
    {
        board = boardRef as IBoardReadonly;
        score = scoreRef as IScore;
        if (board == null) Debug.LogError("[JackPenaltyRule] boardRef는 IBoardReadonly를 구현해야 합니다.");
        if (score == null) Debug.LogError("[JackPenaltyRule] scoreRef는 IScore를 구현해야 합니다.");
    }
    void OnEnable()
    {
        RebuildFromScene();                    // 씬에 이미 있는 잭 스캔
        if (jackFactory) jackFactory.Created += OnJackCreated;  // 런타임 생성 대응
    }
    void OnDisable()
    {
        if (jackFactory) jackFactory.Created -= OnJackCreated;
    }

    void OnJackCreated(Vector3Int cell, JackVisuals jv)
    {
        if (jv != null) _jackByCell[cell] = jv;
    }

    void RebuildFromScene()
    {
        _jackByCell.Clear();
        var all = FindObjectsOfType<JackVisuals>(includeInactive: false);
        foreach (var jv in all)
        {
            // JackVisuals.SetCell(cell)이 반드시 호출되어 있어야 함
            _jackByCell[jv.Cell] = jv;
        }
    }
    public void OnGemsDestroyed(IReadOnlyCollection<Vector3Int> destroyed)
    {
        if (board == null || score == null || destroyed == null || destroyed.Count == 0)
            return;

        var touchedJacks = new HashSet<Vector3Int>();
        foreach (var cell in destroyed)
            foreach (var n in board.Neighbor6(cell))
                if (board.IsBlocked(n))
                    touchedJacks.Add(n);

        if (touchedJacks.Count == 0) return;

        // 1) 잭 본체 Hit
        foreach (var jc in touchedJacks)
        {
            SoundManager.I.PlaySfx(SfxId.JackFly);
        }

        // 2) 토큰 날리고 도착 시 감점
        var jackWorldPositions = new List<Vector3>(touchedJacks.Count);
        foreach (var jc in touchedJacks)
            jackWorldPositions.Add(board.WorldCenterOf(jc));

        vfx.PlayForJacksParallel(jackWorldPositions, () => score.Decrement(1));
    }
}

