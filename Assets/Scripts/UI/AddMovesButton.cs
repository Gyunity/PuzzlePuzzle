using UnityEngine;
using UnityEngine.UI;

public class AddMovesButton : MonoBehaviour
{
    [SerializeField] private MoveCountService moveCount;
    [SerializeField] private Button button;
    [SerializeField] private int addAmount = 5;


    void Awake()
    {
        if (!button) button = GetComponent<Button>();
    }

    void OnEnable()
    {
        if (button) button.onClick.AddListener(OnClick);
        RefreshInteractable();
    }

    void OnDisable()
    {
        if (button) button.onClick.RemoveListener(OnClick);
    }

    void OnClick()
    {
        if (moveCount == null) return;

        moveCount.Add(addAmount);

        SoundManager.I.PlaySfx(SfxId.Button);

        RefreshInteractable();
    }

    void RefreshInteractable()
    {
        if (!button) return;

        button.interactable = true;
    }

}