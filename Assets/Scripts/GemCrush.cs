using UnityEngine;

public class GemCrush : MonoBehaviour
{
    [SerializeField]
    private Renderer[] crushParticle;

    [Header("색깔별 보석조각(Texture)")]
    [SerializeField]
    private Texture redGemCrush;
    [SerializeField]
    private Texture greenGemCrush;
    [SerializeField]
    private Texture orangeGemCrush;
    [SerializeField]
    private Texture pinkGemCrush;
    [SerializeField]
    private Texture purpleGemCrush;
    [SerializeField]
    private Texture yellowGemCrush;

    public void Init(GemType type)
    {
        foreach (var p in crushParticle)
        {
            p.material.SetTexture("_MainTex", GetTexture(type));

        }

    }
    private Texture GetTexture(GemType type)
    {
        return type switch
        {
            GemType.Red => redGemCrush,
            GemType.Green => greenGemCrush,
            GemType.Orange => orangeGemCrush,
            GemType.Pink => pinkGemCrush,
            GemType.Purple => purpleGemCrush,
            GemType.Yellow => yellowGemCrush
        };
    }
}
