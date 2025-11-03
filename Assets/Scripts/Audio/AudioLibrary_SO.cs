using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/AudioLibrary")]
public class AudioLibrary_SO : ScriptableObject
{
    [Serializable] 
    public class SfxEntry { public SfxId id; public List<AudioClip> clips = new(); [Range(0f, 1f)] public float vol = 1f; [Range(-3f, 3f)] public float pitch = 1f; }
    [Serializable] 
    public class BgmEntry { public BgmId id; public AudioClip clip; [Range(0f, 1f)] public float vol = 1f; }

    public List<SfxEntry> sfx = new();
    public List<BgmEntry> bgm = new();

    public bool TryGetSfx(SfxId id, out SfxEntry e) { e = sfx.Find(x => x.id == id); return e != null; }
    public bool TryGetBgm(BgmId id, out BgmEntry e) { e = bgm.Find(x => x.id == id); return e != null; }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (sfx != null)
        {
            foreach (var e in sfx)
            {
                if (e == null) continue;
                if (Mathf.Approximately(e.vol, 0f)) e.vol = 1f;
                if (Mathf.Approximately(e.pitch, 0f)) e.pitch = 1f;
                if (e.clips == null || e.clips.Count == 0)
                    Debug.LogWarning($"[AudioLibrary] SFX {e.id} has no clips!");
            }
        }
    }
#endif
}