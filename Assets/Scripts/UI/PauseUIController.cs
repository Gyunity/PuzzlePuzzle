using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseUIController : MonoBehaviour
{
    [SerializeField] 
    private MonoBehaviour pauseRef;
    [SerializeField] 
    private HintSystem hintSystem;
    [SerializeField] 
    private Toggle hintToggle;
    [SerializeField] 
    private Button pauseBtn, btnContinue, btnRestart, btnQuit;
    [SerializeField] 
    private UIPopup pausePopup;  

    private IPauseService pause;

    void Awake()
    {
        pause = pauseRef as IPauseService;
        if (pause == null) Debug.LogError("[PauseUI] pauseRef must implement IPauseService");

        if (pausePopup == null) Debug.LogError("[PauseUI] pausePopup not assigned");
        else
        {
            // 부모가 꺼져 있으면 자식만 Show해도 안 보이므로 선제 활성화
            ForceActivateAncestors(pausePopup.transform);
        }
    }

    void OnEnable()
    {
        if (pause != null) { pause.Paused += OnPaused; pause.Resumed += OnResumed; }
        if (pauseBtn) pauseBtn.onClick.AddListener(() => pause?.Pause());
        if (btnContinue) btnContinue.onClick.AddListener(() => pause?.Resume());
        if (btnRestart) btnRestart.onClick.AddListener(OnClickRestart);
        if (btnQuit) btnQuit.onClick.AddListener(OnClickQuit);
        if (hintToggle) hintToggle.onValueChanged.AddListener(OnHintToggled);
    }

    void OnDisable()
    {
        if (pause != null) { pause.Paused -= OnPaused; pause.Resumed -= OnResumed; }
        if (pauseBtn) pauseBtn.onClick.RemoveAllListeners();
        if (btnContinue) btnContinue.onClick.RemoveAllListeners();
        if (btnRestart) btnRestart.onClick.RemoveAllListeners();
        if (btnQuit) btnQuit.onClick.RemoveAllListeners();
        if (hintToggle) hintToggle.onValueChanged.RemoveListener(OnHintToggled);
    }

    void OnPaused()
    {
        SoundManager.I.PlaySfx(SfxId.Button);

        if (pausePopup)
        {
            ForceActivateAncestors(pausePopup.transform); 
            pausePopup.Show();                            
        }
        // 힌트 토글 상태 동기화
        if (hintToggle && hintSystem)
        {
            var on = PlayerPrefs.GetInt("HintsEnabled", 1) == 1;
            hintToggle.isOn = on;
            hintSystem.SetMasterEnabled(on);
        }
    }

    void OnResumed()
    {
        SoundManager.I.PlaySfx(SfxId.Button);

        pausePopup?.Hide();
    }

    void OnClickRestart()
    {
        SoundManager.I.PlaySfx(SfxId.Button);

        AudioListener.pause = false;

        // 안전 정리
        Time.timeScale = 1f;
        ScaleTweener.Instance?.CancelAll();

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
    }

    void OnClickQuit()
    {

        pause?.Resume();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnHintToggled(bool on)
    {
        if (hintSystem) hintSystem.SetMasterEnabled(on);
        PlayerPrefs.SetInt("HintsEnabled", on ? 1 : 0);
        PlayerPrefs.Save();
    }

    static void ForceActivateAncestors(Transform t)
    {
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            t = t.parent;
        }
    }
}