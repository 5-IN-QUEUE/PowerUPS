using TMPro;
using UnityEngine;
using UnityEngine.UI;

// kawaii_creatureScene의 InputField(TMP) + MakeRoom 버튼에 연결해서 사용.
// 같은 방 이름을 입력한 두 사람 중 먼저 누른 사람이 방을 만들고(호스트), 나중 사람은 그 방에 입장(클라이언트)한다.
public class MatchmakingUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Button makeRoomButton;

    [Header("선택 사항 (없으면 비워둬도 됨)")]
    [SerializeField] private Button cancelButton;
    [SerializeField] private GameObject searchingPanel;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Start()
    {
        makeRoomButton.onClick.AddListener(() => NetworkLauncher.Instance.JoinOrCreateRoom(roomNameInput.text));
        if (cancelButton != null)
            cancelButton.onClick.AddListener(() => NetworkLauncher.Instance.CancelMatchmaking());

        NetworkLauncher.Instance.OnMatchStateChanged += HandleStateChanged;
        NetworkLauncher.Instance.OnPlayerCountChanged += HandlePlayerCountChanged;

        HandleStateChanged(NetworkLauncher.Instance.State);
    }

    private void OnDestroy()
    {
        if (NetworkLauncher.Instance == null) return;
        NetworkLauncher.Instance.OnMatchStateChanged -= HandleStateChanged;
        NetworkLauncher.Instance.OnPlayerCountChanged -= HandlePlayerCountChanged;
    }

    private void HandleStateChanged(NetworkLauncher.MatchState state)
    {
        switch (state)
        {
            case NetworkLauncher.MatchState.Idle:
                if (searchingPanel != null) searchingPanel.SetActive(false);
                makeRoomButton.interactable = true;
                roomNameInput.interactable = true;
                break;

            case NetworkLauncher.MatchState.Searching:
                if (searchingPanel != null) searchingPanel.SetActive(true);
                makeRoomButton.interactable = false;
                roomNameInput.interactable = false;
                SetStatusText($"'{NetworkLauncher.Instance.CurrentRoomName}' 방 입장 중...");
                break;

            case NetworkLauncher.MatchState.Found:
                if (cancelButton != null) cancelButton.interactable = false;
                SetStatusText("매칭 완료!");
                break;

            case NetworkLauncher.MatchState.Failed:
                if (searchingPanel != null) searchingPanel.SetActive(false);
                makeRoomButton.interactable = true;
                roomNameInput.interactable = true;
                if (cancelButton != null) cancelButton.interactable = true;
                SetStatusText("방 입장/생성에 실패했습니다. 다시 시도해주세요.");
                break;
        }
    }

    private void HandlePlayerCountChanged(int current, int required)
    {
        if (NetworkLauncher.Instance.State != NetworkLauncher.MatchState.Searching) return;
        SetStatusText($"'{NetworkLauncher.Instance.CurrentRoomName}' 상대 기다리는 중... ({current}/{required})");
    }

    private void SetStatusText(string text)
    {
        if (statusText != null) statusText.text = text;
    }
}
