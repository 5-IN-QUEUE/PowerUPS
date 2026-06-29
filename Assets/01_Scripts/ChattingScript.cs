using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChattingScript : MonoBehaviour  // ✅ MonoBehaviour로 충분
{
    public static ChattingScript Instance { get; private set; }  // ✅ 싱글톤

    [SerializeField] private GameObject chatPrefab;
    [SerializeField] private Transform  textGroup;
    [SerializeField] private InputField inputField;
    [SerializeField] private int        maxMessages = 9;

    // ✅ 채팅창 열려있는지 외부(InputHandler)에서 읽어감
    public bool IsChatOpen { get; private set; } = false;

    private readonly List<GameObject> _messageQueue = new();

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // ✅ Inspector 직접 연결 권장, 없으면 폴백
        if (textGroup  == null) textGroup  = transform.GetChild(0);
        if (inputField == null) inputField = transform.GetChild(1).GetComponent<InputField>();

        // ✅ onEndEdit 대신 submitString 방식
        inputField.onEndEdit.AddListener(OnEndEdit);
    }

    private void Update()
    {
        // T키: 채팅창 열기
        if (Input.GetKeyDown(KeyCode.T) && !IsChatOpen)
            OpenChat();

        // ESC키: 채팅창 닫기
        if (Input.GetKeyDown(KeyCode.Escape) && IsChatOpen)
            CloseChat();
    }

    // ✅ onEndEdit은 value만 받고 엔터 여부는 submitString으로 판단
    private void OnEndEdit(string value)
    {
        // InputField의 lineType이 Single이면 엔터 = submit
        if (!string.IsNullOrWhiteSpace(value))
            SendMessage_Internal(value);

        CloseChat();
    }

    public void OnSendButtonClicked()
    {
        if (string.IsNullOrWhiteSpace(inputField.text)) return;
        SendMessage_Internal(inputField.text);
        CloseChat();
    }

    private void SendMessage_Internal(string message)
    {
        if (PlayerController.localPlayer == null) return;

        var pc = PlayerController.localPlayer.GetComponent<PlayerController>();
        pc.RPC_SendChatMessage(pc.PlayerName.ToString(), message);

        inputField.text = "";
    }

    public void AddChatMessage(string senderName, string message)
    {
        // 최대 메시지 초과 시 가장 오래된 것 제거
        if (_messageQueue.Count >= maxMessages)
        {
            Destroy(_messageQueue[0]);
            _messageQueue.RemoveAt(0);
        }

        var newMsg = Instantiate(chatPrefab, textGroup);
        newMsg.GetComponent<Text>().text = $"[{senderName}] {message}";
        _messageQueue.Add(newMsg);
    }

    // ==================== 채팅창 열기/닫기 ====================

    private void OpenChat()
    {
        IsChatOpen = true;
        inputField.ActivateInputField();
        inputField.Select();
    }

    private void CloseChat()
    {
        IsChatOpen = false;
        inputField.DeactivateInputField();
        inputField.text = "";
    }
}