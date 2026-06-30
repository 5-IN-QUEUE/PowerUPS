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

    private CardLayout[] slotLayouts = new CardLayout[4];
    private int currentIdx = 0;
    private int animatingCount = 0;

    void Start()
    {
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

        UpdateLayout(animated: false);
    }

    void Update()
    {
        if (animatingCount > 0) return;

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

        // 엔터 키로 카드 확정
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
        Debug.Log($"[UpgradeCardSelect] Card confirmed: {currentIdx}");
        OnCardConfirmed?.Invoke(currentIdx);
        gameObject.SetActive(false);
    }
}
