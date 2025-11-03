using System.Collections;
using UnityEngine;

/// 잭 객체(장애물)에 붙여서 '맞았을 때' 짧은 연출을 수행
public class JackVisuals : MonoBehaviour
{
    [Header("Optional Animator")]
    [SerializeField] Animator animator;


    // 잭이 박혀있는 보드 셀 (팩토리에서 셋업)
    public Vector3Int Cell { get; private set; }
    public void SetCell(Vector3Int cell) => Cell = cell;

    public Transform Anchor => transform; 

    public void PlayHit()
    {
        if (animator)
            animator.SetTrigger("Hit");
       
    }

}