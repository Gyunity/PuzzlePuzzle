using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//공용 인터페이스
public interface IBoardReadonly
{
    bool TryPeekGem(Vector3Int cell, out Gem gem);
    bool IsBlocked(Vector3Int cell);
    IEnumerable<Vector3Int> Neighbor6(Vector3Int cell);
    bool TryFindHintDetailed(out Vector3Int from, out Vector3Int to, out Vector3Int anchorA, out Vector3Int anchorB);
    Vector3 WorldCenterOf(Vector3Int cell);
}

public interface IDestructionListener
{
    //이번 프레임에 파괴된 셀들
    void OnGemsDestroyed(IReadOnlyCollection<Vector3Int> destroyed);
}
public interface ICountListener
{
    //이번 프레임에 파괴된 셀들
    void MovingGem(List<Vector3Int> match);
}

public interface IScore
{
    int Current {  get; }
    event Action<int> Changed;
    event Action Cleared;
    void Decrement(int amount = 1);
}

public interface IMoveCount
{
    int Current { get; }
    event Action<int> Changed;
    event Action Done;
    void Decrement(int amount = 1);
}

public interface IPoints
{
    int Current { get; }
    event System.Action<int> Changed;
    void Add(int amount);
    void Reset(int value = 0);
}
public interface IPauseService
{
    bool IsPaused { get; }
    event System.Action Paused;
    event System.Action Resumed;

    void Pause();
    void Resume();
    void Toggle();
}

public interface IResettable
{
    void ResetState();
}

public interface ISoundService
{

    // 0~1 (마스터 SFX)
    float SfxVolume { get; set; }
    // 0~1 (마스터 BGM)
    float BgmVolume { get; set; } 
    bool Muted { get; set; }

    void PlaySfx(SfxId id, float volScale = 1f, float pitch = 1f);
    void PlaySfxAt(SfxId id, Vector3 worldPos, float volScale = 1f, float pitch = 1f); 
    void PlayUi(SfxId id);

    void PlayBgm(BgmId id, float fade = 0.8f);
    void StopBgm(float fade = 0.5f);
}