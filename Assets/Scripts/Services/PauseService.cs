using UnityEngine;

public class PauseService : MonoBehaviour, IPauseService
{
    [SerializeField] private bool pauseAudioListener = false;

    public bool IsPaused { get; private set; }
    public event System.Action Paused;
    public event System.Action Resumed;

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;
        Debug.Log("[PauseService] Paused fired");

        if (pauseAudioListener) AudioListener.pause = true;
        Paused?.Invoke();
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
        if (pauseAudioListener) AudioListener.pause = false;
        Resumed?.Invoke();
    }

    public void Toggle()
    {
        if (IsPaused) Resume();
        else Pause();
    }
}
