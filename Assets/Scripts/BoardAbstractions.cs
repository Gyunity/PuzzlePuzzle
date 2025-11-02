using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//공용 인터페이스
public interface IBoardReadonly
{
    bool IsBlocked(Vector3Int cell);
    IEnumerable<Vector3Int> Neighbor6(Vector3Int cell);
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