using UnityEngine;
using TMPro;
using System.Collections;

public class UpgradeCardSelect : MonoBehaviour
{
    public RectTransform[] upgradeCards;

    public float animDuration;

    public event System.Action<int> OnCardConfirmed;

    private struct TextLayout
    {
        public Vector2 anchoredPos;
        public Vector2 sizeDelta;
        public float fontSize;
    }

    private struct CardLayout
    {
        public Vector3 worldPos;
        public Vector2 sizeDelta;
        public TextLayout nameLayout;
        public TextLayout descLayout;
    }

    public static bool IsSelecting { get; private set; } = false;

    private CardLayout[] slotLayouts = new CardLayout[4];
    private int currentIdx = 0;
    private int animatingCount = 0;
    private bool _layoutsRecorded = false;
    private bool _confirmed = false;

    void Start() { }

    // 비활성 오브젝트는 Start()가 호출되지 않으므로
    // 최초 OnEnable()에서 카드 초기 위치를 기록하고 이후에는 상태만 리셋
    void OnEnable()
    {
        if (!_layoutsRecorded)
        {
            if (upgradeCards == null || upgradeCards.Length < 4) return;
            for (int i = 0; i < 4; i++)
            {
                var nameRect = upgradeCards[i].GetChild(0).GetComponent<RectTransform>();
                var descRect = upgradeCards[i].GetChild(1).GetComponent<RectTransform>();

                slotLayouts[i] = new CardLayout
                {
                    worldPos  = upgradeCards[i].position,
                    sizeDelta = upgradeCards[i].sizeDelta,

                    nameLayout = new TextLayout
                    {
                        anchoredPos = nameRect.anchoredPosition,
                        sizeDelta   = nameRect.sizeDelta,
                        fontSize    = nameRect.GetComponent<TextMeshProUGUI>().fontSize
                    },
                    descLayout = new TextLayout
                    {
                        anchoredPos = descRect.anchoredPosition,
                        sizeDelta   = descRect.sizeDelta,
                        fontSize    = descRect.GetComponent<TextMeshProUGUI>().fontSize
                    }
                };
            }
            _layoutsRecorded = true;
        }

        IsSelecting = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        _confirmed = false;
        currentIdx = 0;
        StopAllCoroutines();
        animatingCount = 0;
        UpdateLayout(animated: false);
    }

    void OnDisable()
    {
        IsSelecting = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void Update()
    {
        if (animatingCount > 0 || _confirmed) return;

        if (Input.GetKeyDown(KeyCode.A))
        {
            currentIdx = (currentIdx - 1 + 4) % 4;
            UpdateLayout(animated: true);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            currentIdx = (currentIdx + 1) % 4;
            UpdateLayout(animated: true);
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            ConfirmCard();
        }
    }

    void UpdateLayout(bool animated)
    {
        for (int i = 0; i < 4; i++)
        {
            int slotIdx = (i - currentIdx + 4) % 4;
            CardLayout targetLayout = slotLayouts[slotIdx];

            if (animated)
                StartCoroutine(AnimateCard(upgradeCards[i], targetLayout));
            else
                ApplyLayout(upgradeCards[i], targetLayout);
        }
    }

    void ApplyLayout(RectTransform card, CardLayout layout)
    {
        var nameRect = card.GetChild(0).GetComponent<RectTransform>();
        var descRect = card.GetChild(1).GetComponent<RectTransform>();

        card.position    = layout.worldPos;
        card.sizeDelta   = layout.sizeDelta;

        nameRect.anchoredPosition = layout.nameLayout.anchoredPos;
        nameRect.sizeDelta        = layout.nameLayout.sizeDelta;
        nameRect.GetComponent<TextMeshProUGUI>().fontSize = layout.nameLayout.fontSize;

        descRect.anchoredPosition = layout.descLayout.anchoredPos;
        descRect.sizeDelta        = layout.descLayout.sizeDelta;
        descRect.GetComponent<TextMeshProUGUI>().fontSize = layout.descLayout.fontSize;
    }

    IEnumerator AnimateCard(RectTransform card, CardLayout targetLayout)
    {
        animatingCount++;

        var nameRect = card.GetChild(0).GetComponent<RectTransform>();
        var descRect = card.GetChild(1).GetComponent<RectTransform>();

        Vector3 startCardPos     = card.position;
        Vector2 startCardSize    = card.sizeDelta;

        Vector2 startNamePos     = nameRect.anchoredPosition;
        Vector2 startNameSize    = nameRect.sizeDelta;
        float   startNameFont    = nameRect.GetComponent<TextMeshProUGUI>().fontSize;

        Vector2 startDescPos     = descRect.anchoredPosition;
        Vector2 startDescSize    = descRect.sizeDelta;
        float   startDescFont    = descRect.GetComponent<TextMeshProUGUI>().fontSize;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / animDuration;
            float ease = Mathf.SmoothStep(0f, 1f, t);

            card.position    = Vector3.Lerp(startCardPos, targetLayout.worldPos, ease);
            card.sizeDelta   = Vector2.Lerp(startCardSize, targetLayout.sizeDelta, ease);

            nameRect.anchoredPosition = Vector2.Lerp(startNamePos, targetLayout.nameLayout.anchoredPos, ease);
            nameRect.sizeDelta        = Vector2.Lerp(startNameSize, targetLayout.nameLayout.sizeDelta, ease);
            nameRect.GetComponent<TextMeshProUGUI>().fontSize = Mathf.Lerp(startNameFont, targetLayout.nameLayout.fontSize, ease);

            descRect.anchoredPosition = Vector2.Lerp(startDescPos, targetLayout.descLayout.anchoredPos, ease);
            descRect.sizeDelta        = Vector2.Lerp(startDescSize, targetLayout.descLayout.sizeDelta, ease);
            descRect.GetComponent<TextMeshProUGUI>().fontSize = Mathf.Lerp(startDescFont, targetLayout.descLayout.fontSize, ease);

            yield return null;
        }

        ApplyLayout(card, targetLayout);
        animatingCount--;
    }

    public void ConfirmCard()
    {
        if (_confirmed) return;
        _confirmed = true;

        Debug.Log($"[UpgradeCardSelect] Card confirmed: {currentIdx}");
        OnCardConfirmed?.Invoke(currentIdx);

        if (PowerUpManager.Instance != null)
            PowerUpManager.Instance.OnCardConfirmed(currentIdx);

        // SetActive(false)를 여기서 호출하면 안 됨:
        // 부모 패널(upgradeCardPanel)이 다음 라운드에 SetActive(true)될 때
        // 자식인 이 오브젝트는 여전히 inactive 상태라 OnEnable이 호출되지 않아
        // 2라운드부터 카드 선택이 완전히 죽음.
        // 패널 숨김은 UIManager가 RoundActive 상태 진입 시 처리한다.
    }
}
