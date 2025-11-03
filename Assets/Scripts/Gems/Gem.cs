using UnityEngine;
public enum SpecialKind { None, LineBlaster }

public class Gem : MonoBehaviour
{
    [Header("색깔별 보석들(Sprite)")]
    [SerializeField]
    private Sprite redGem;
    [SerializeField]
    private Sprite greenGem;
    [SerializeField] 
    private Sprite orangeGem;
    [SerializeField] 
    private Sprite pinkGem;
    [SerializeField]
    private Sprite purpleGem;
    [SerializeField]
    private Sprite yellowGem;
    [SerializeField]
    private GameObject speEffect;
    // 특수 속성
    public SpecialKind Special { get; private set; } = SpecialKind.None;
    public int BlastAxis { get; private set; } = -1; 

    public GemType GemType { get; private set; }

    public Texture GemTexture { get; private set; }

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Init(GemType type)
    {
        GemType = type;
        Special = SpecialKind.None;
        BlastAxis = -1;
        spriteRenderer.sprite = GetSprite(type);
    }
    public void SetLineBlaster(int axis)
    {
        Special = SpecialKind.LineBlaster;
        BlastAxis = axis;
        speEffect.SetActive(true);
       
    }
    public bool IsLineBlaster() => Special == SpecialKind.LineBlaster;

    private Sprite GetSprite(GemType type)
    {
        return type switch
        {
            GemType.Red => redGem,
            GemType.Green => greenGem,
            GemType.Orange => orangeGem,
            GemType.Pink => pinkGem,
            GemType.Purple => purpleGem,
            GemType.Yellow => yellowGem
        };
    }
 


}
