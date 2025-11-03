using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HudUI : MonoBehaviour
{
    [Header("Score(잭)")]
    [SerializeField] private JackScoreService score;
    [SerializeField] private TMP_Text scoreLabel;
    private Action<int> _onScoreChanged;
    private Action _onScoreCleared;

    [Header("moveCount(이동 횟수)")]
    [SerializeField] private MoveCountService moveCount;
    [SerializeField] private TMP_Text movesLabel;
    private Action<int> _onMovesChanged;
    private Action _onMovesExhausted;

    [Header("Points(점수)")]
    [SerializeField] private PointsService points;
    [SerializeField] private List<TMP_Text> pointLabel = new();
    [SerializeField] private Slider pointSlider;
    [SerializeField] private int pointTarget = 5500;
    private Action<int> _onPointChanged;

    [Header("End Popups")]
    [SerializeField] private UIPopup clearPopup;   
    [SerializeField] private UIPopup failPopup;    

    [Header("Buttons (optional)")]
    [SerializeField] private Button btnClearRestart;
    [SerializeField] private Button btnFailRestart;

    void Awake()
    {
        // 라벨 바인딩
        _onScoreChanged = v => { if (scoreLabel) scoreLabel.text = v.ToString(); };
        _onScoreCleared = () => { if (scoreLabel) scoreLabel.text = 0.ToString(); };
        _onMovesChanged = v => { if (movesLabel) movesLabel.text = v.ToString(); };
        _onMovesExhausted = () => { if (movesLabel) movesLabel.text = 0.ToString(); };
        _onPointChanged = v =>
        {
            if (pointLabel.Count > 0)
                foreach (var t in pointLabel) t.text = v.ToString();
            if (pointSlider) pointSlider.value = v;
        };

        // 팝업 부모 Active 보장
        ForceActivateAncestors(clearPopup ? clearPopup.transform : null);
        ForceActivateAncestors(failPopup ? failPopup.transform : null);
    }

    void Start()
    {
        if (pointSlider) pointSlider.maxValue = pointTarget;

        // 엔드 버튼 핸들러 
        if (btnClearRestart) btnClearRestart.onClick.AddListener(RestartScene);
        if (btnFailRestart) btnFailRestart.onClick.AddListener(RestartScene);
    }

    void OnEnable()
    {
        if (score)
        {
            score.Changed += _onScoreChanged;
            score.Cleared += OnScoreCleared_ShowClear;   
            _onScoreChanged(score.Current);
        }
        if (moveCount)
        {
            moveCount.Changed += _onMovesChanged;
            moveCount.Done += OnMovesExhausted_ShowEnd;
            _onMovesChanged(moveCount.Current);
        }
        if (points)
        {
            points.Changed += _onPointChanged;
            _onPointChanged(points.Current);
        }
    }

    void OnDisable()
    {
        if (score)
        {
            score.Changed -= _onScoreChanged;
            score.Cleared -= OnScoreCleared_ShowClear;
        }
        if (moveCount)
        {
            moveCount.Changed -= _onMovesChanged;
            moveCount.Done -= OnMovesExhausted_ShowEnd;
        }
        if (points)
        {
            points.Changed -= _onPointChanged;
        }

        if (btnClearRestart) btnClearRestart.onClick.RemoveAllListeners();
        if (btnFailRestart) btnFailRestart.onClick.RemoveAllListeners();
    }

    

    // 잭 점수가 0이 되었을 때 클리어
    void OnScoreCleared_ShowClear()
    {
        ShowClearPopup();
    }

    // 이동이 모두 소진되었을 때 
    void OnMovesExhausted_ShowEnd()
    {
        // 잭 점수 0 이하면 클리어
        // 아니면 실패 
        bool isCleared = score != null && score.Current <= 0;


        if (isCleared) ShowClearPopup();
        else ShowFailPopup();
    }

    

    void ShowClearPopup()
    {
        if (clearPopup)
        {
            SoundManager.I.PlaySfx(SfxId.Clear);

            ForceActivateAncestors(clearPopup.transform);
            clearPopup.Show(); 
        }
    }

    void ShowFailPopup()
    {
        if (failPopup)
        {
            SoundManager.I.PlaySfx(SfxId.Button);

            ForceActivateAncestors(failPopup.transform);
            failPopup.Show();
        }
    }

   

    static void ForceActivateAncestors(Transform t)
    {
        if (!t) return;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            t = t.parent;
        }
    }

    void RestartScene()
    {

        // 안전 정리
        Time.timeScale = 1f;
        ScaleTweener.Instance?.CancelAll();

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
    }

}