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

    void Awake()
    {
        RecordLayoutsIfNeeded();
    }

    // 이 컴포넌트가 붙은 오브젝트가 씬에서 UIManager가 켜고 끄는 카드 패널의
    // 자식이 아니라 별도로 배치되어 있을 수 있으므로(항상 활성 상태),
    // 카드 선택 on/off는 오브젝트의 OnEnable/OnDisable이 아니라
    // GameFlowManager의 상태 변화 이벤트로 직접 제어한다.
    void OnEnable()
    {
        GameFlowManager.OnStateChanged += HandleGameStateChanged;

        if (GameFlowManager.Instance != null &&
            GameFlowManager.Instance.CurrentState == GameFlowManager.GameState.AugmentSelect)
        {
            BeginSelecting();
        }
    }

    void OnDisable()
    {
        GameFlowManager.OnStateChanged -= HandleGameStateChanged;
        EndSelecting();
    }

    private void HandleGameStateChanged(GameFlowManager.GameState state)
    {
        if (state == GameFlowManager.GameState.AugmentSelect)
            BeginSelecting();
        else
            EndSelecting();
    }

    private void RecordLayoutsIfNeeded()
    {
        if (_layoutsRecorded) return;
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

    public void BeginSelecting()
    {
        RecordLayoutsIfNeeded();

        IsSelecting = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        _confirmed = false;
        currentIdx = 0;
        StopAllCoroutines();
        animatingCount = 0;
        UpdateLayout(animated: false);
    }

    private void EndSelecting()
    {
        IsSelecting = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void Update()
    {
        if (!IsSelecting) return;
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

        if (PowerUpManager.Instance == null)
        {
            // 아직 PowerUpManager가 스폰되지 않은 상태 — _confirmed를 세우지 않고
            // 다음 Enter 입력에서 다시 시도할 수 있게 둔다. 여기서 true로 확정해버리면
            // 이 클라이언트는 선택을 서버에 영영 전송하지 못한 채 잠겨서
            // 상대방도 함께 다음 라운드로 못 넘어가는 소프트락이 발생한다.
            Debug.LogWarning("[UpgradeCardSelect] PowerUpManager.Instance가 아직 없어 선택 전송을 보류합니다.");
            return;
        }

        _confirmed = true;

        Debug.Log($"[UpgradeCardSelect] Card confirmed: {currentIdx}");
        OnCardConfirmed?.Invoke(currentIdx);
        PowerUpManager.Instance.OnCardConfirmed(currentIdx);

        // EndSelecting()을 여기서 직접 부르지 않는다: 실제 선택 종료는
        // GameFlowManager가 RoundActive로 전환되며 보내는 OnStateChanged 이벤트로 처리된다
        // (두 플레이어가 모두 선택을 마쳐야 서버가 상태를 전환하기 때문).
        // 카드 UI 패널 표시/숨김은 UIManager가 별도로 처리한다.
    }
}
