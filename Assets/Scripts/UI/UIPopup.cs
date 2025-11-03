using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class UIPopup : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Min(0f)] 
    float panelIn = 0.15f;
    [SerializeField, Min(0f)] 
    float panelOut = 0.12f;
    [SerializeField] 
    EaseType panelEaseIn = EaseType.EaseOutBack;
    [SerializeField]
    EaseType panelEaseOut = EaseType.EaseInOutQuad;

    [Header("Buttons pop")]
    [SerializeField] 
    bool animateButtons = true;
    [SerializeField, Min(0f)]
    float btnDelay = 0.03f;
    [SerializeField, Min(0f)] 
    float btnDur = 0.10f;
    [SerializeField] 
    EaseType btnEase = EaseType.EaseOutBack;

    [Header("Time")]
    [SerializeField]
    bool useUnscaledTime = true; 

    Transform[] _buttons;

    void Awake()
    {
        _buttons = GetComponentsInChildren<Button>(true).Select(b => b.transform).ToArray();
        gameObject.SetActive(false);
        transform.localScale = Vector3.zero;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (ScaleTweener.Instance)
            ScaleTweener.Instance.PopIn(transform, panelIn, useUnscaledTime, ease: panelEaseIn);

        if (animateButtons && _buttons != null && _buttons.Length > 0)
            StartCoroutine(Co_PopButtons());
    }

    public void Hide()
    {
        if (!gameObject.activeSelf) return;
        if (ScaleTweener.Instance)
            ScaleTweener.Instance.PopOut(transform, panelOut, useUnscaledTime, deactivateAtEnd: true, ease: panelEaseOut);
    }

    IEnumerator Co_PopButtons()
    {
        // 버튼들 스케일 0으로 초기화
        foreach (var t in _buttons) t.localScale = Vector3.zero;

        // 순차 팝업
        for (int i = 0; i < _buttons.Length; ++i)
        {
            if (ScaleTweener.Instance)
                ScaleTweener.Instance.PopIn(_buttons[i], btnDur, useUnscaledTime, ease: btnEase);
            float d = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float wait = btnDelay;
            while (wait > 0f) { wait -= useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; yield return null; }
        }
    }
}
