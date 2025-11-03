using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour, ISoundService
{
    public static ISoundService I { get; private set; }

    [Header("Data")]
    [SerializeField] private AudioLibrary_SO library;

    [Header("Volumes")]
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float bgmVolume = 0.8f;
    [SerializeField] private bool muted = false;

    [Header("SFX Pool")]
    [SerializeField] private int sfxPoolSize = 12;
    [SerializeField] private bool sfx3D = false; 
    private readonly List<AudioSource> _sfxPool = new();
    private int _sfxHead;

    [Header("BGM")]
    [SerializeField] private AudioSource bgmA;
    [SerializeField] private AudioSource bgmB;
    private AudioSource _activeBgm, _idleBgm;

    [SerializeField] private bool forceAtPointFallback = false;

    const string KEY_SFX = "vol_sfx";
    const string KEY_BGM = "vol_bgm";
    const string KEY_MUTE = "vol_mute";

    [SerializeField] private AudioSource sfxBus;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // load prefs
        sfxVolume = PlayerPrefs.GetFloat(KEY_SFX, sfxVolume);
        bgmVolume = PlayerPrefs.GetFloat(KEY_BGM, bgmVolume);
        muted = PlayerPrefs.GetInt(KEY_MUTE, muted ? 1 : 0) == 1;
        AudioListener.pause = false;
        // SFX pool
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var a = new GameObject($"SFX_{i}").AddComponent<AudioSource>();
            a.transform.SetParent(transform, false);   
            a.playOnAwake = false;
            a.loop = false;
            a.spatialBlend = sfx3D ? 1f : 0f;
            a.rolloffMode = AudioRolloffMode.Linear;
            a.minDistance = 1f;
            a.maxDistance = 20f;
            a.ignoreListenerPause = false;
            a.bypassListenerEffects = true;
            a.mute = false;
            a.volume = 1f;
            _sfxPool.Add(a);
        }

        // BGM sources 준비
        if (!bgmA) bgmA = new GameObject("BGM_A").AddComponent<AudioSource>();
        if (!bgmB) bgmB = new GameObject("BGM_B").AddComponent<AudioSource>();
        bgmA.transform.SetParent(transform, false);
        bgmB.transform.SetParent(transform, false);
        foreach (var b in new[] { bgmA, bgmB })
        {
            b.playOnAwake = false;
            b.loop = true;
            b.spatialBlend = 0f; 
            b.ignoreListenerPause = true; 
            b.volume = 0f;
        }
        _activeBgm = bgmA; _idleBgm = bgmB;

        AudioListener.pause = false;
        AudioListener.volume = 1f;

    
    }

    void OnDestroy() { if (I == this) I = null; }

    public float SfxVolume { get => sfxVolume; set { sfxVolume = Mathf.Clamp01(value); Save(); } }
    public float BgmVolume { get => bgmVolume; set { bgmVolume = Mathf.Clamp01(value); if (_activeBgm) _activeBgm.volume = bgmVolume; Save(); } }
    public bool Muted { get => muted; set { muted = value; ApplyMute(); Save(); } }

    public void PlaySfx(SfxId id, float volScale = 1f, float pitch = 1f) => PlaySfxInternal(id, null, volScale, pitch);
    public void PlayUi(SfxId id) => PlaySfx(id, 1f, 1f);
    public void PlaySfxAt(SfxId id, Vector3 worldPos, float volScale = 1f, float pitch = 1f) => PlaySfxInternal(id, worldPos, volScale, pitch);



    public void PlayBgm(BgmId id, float fade = 0.8f)
    {
        if (library == null || !library.TryGetBgm(id, out var e) || e.clip == null) return;
        // idle에 세팅
        _idleBgm.clip = e.clip;
        _idleBgm.volume = 0f;
        _idleBgm.Play();

        StopAllCoroutines();
        StartCoroutine(Co_Crossfade(_activeBgm, _idleBgm, (muted ? 0f : bgmVolume) * e.vol, Mathf.Max(0f, fade)));

        // swap
        var tmp = _activeBgm; _activeBgm = _idleBgm; _idleBgm = tmp;
    }

    public void StopBgm(float fade = 0.5f)
    {
        if (!_activeBgm) return;
        StopAllCoroutines();
        StartCoroutine(Co_FadeOut(_activeBgm, Mathf.Max(0f, fade)));
    }
  
    void ApplyMute()
    {
        AudioListener.pause = false; // 시스템 전체 일시정지는 사용 안함
        var vS = muted ? 0f : sfxVolume;
        var vB = muted ? 0f : bgmVolume;
        foreach (var a in _sfxPool) a.volume = vS;
        if (_activeBgm) _activeBgm.volume = vB;
    }

    void Save()
    {
        PlayerPrefs.SetFloat(KEY_SFX, sfxVolume);
        PlayerPrefs.SetFloat(KEY_BGM, bgmVolume);
        PlayerPrefs.SetInt(KEY_MUTE, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    void PlaySfxInternal(SfxId id, Vector3? worldPos, float volScale, float pitch)
    {
        if (library == null || !library.TryGetSfx(id, out var e) || e.clips.Count == 0) return;
        var clip = e.clips[Random.Range(0, e.clips.Count)];
        var a = _sfxPool[_sfxHead]; _sfxHead = (_sfxHead + 1) % _sfxPool.Count;

        a.transform.position = worldPos ?? Vector3.zero;
        a.pitch = e.pitch * pitch;
        a.volume = (muted ? 0f : sfxVolume) * e.vol * volScale;
        a.PlayOneShot(clip);
    }


    System.Collections.IEnumerator Co_Crossfade(AudioSource from, AudioSource to, float targetVol, float dur)
    {
        float t = 0f;
        float from0 = from ? from.volume : 0f;
        float to0 = to.volume;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / dur);
            if (from) from.volume = Mathf.Lerp(from0, 0f, u);
            if (to) to.volume = Mathf.Lerp(to0, targetVol, u);
            yield return null;
        }
        if (from) { from.volume = 0f; from.Stop(); }
        if (to) to.volume = targetVol;
    }

    System.Collections.IEnumerator Co_FadeOut(AudioSource a, float dur)
    {
        float t = 0f; float v0 = a.volume;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / dur);
            a.volume = Mathf.Lerp(v0, 0f, u);
            yield return null;
        }
        a.volume = 0f; a.Stop();
    }
}