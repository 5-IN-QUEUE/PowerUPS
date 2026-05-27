using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChattingScript : NetworkBehaviour  // ✅ NetworkBehaviour로 변경
{
    public GameObject ChatPrefab;
    private Transform textGroup;
    private InputField inputText;
    private List<GameObject> chattingQueue;

    void Awake()
    {
        textGroup = transform.GetChild(0);
        inputText = transform.GetChild(1).GetComponent<InputField>();
        chattingQueue = new List<GameObject>();
    }
    public override void FixedUpdateNetwork()
    {
        EventSystem.current.SetSelectedGameObject(inputText.gameObject);
        if (Input.GetKey(KeyCode.KeypadEnter))
        {
            OnSendButtonClicked();
        };
    }

    // ✅ 버튼 OnClick에서 이걸 호출
    public void OnSendButtonClicked(){
        if (PlayerController.localPlayer == null) return; // 안전 체크
        if (string.IsNullOrEmpty(inputText.text)) return; // 빈 메시지 방지
        PlayerController.localPlayer
        .GetComponent<PlayerController>()
        .RPC_SendChatMessage(PlayerController.localPlayer.name, inputText.text);

        inputText.text = ""; // 입력창 초기화
    }
    public void AddChatMessage(string senderName, string message)
    {
        GameObject newPrefab = Instantiate(ChatPrefab, textGroup);
        newPrefab.transform.GetComponent<Text>().text 
            = $"[{senderName}] {message}";
        if(chattingQueue.Count >= 9){
            Destroy(chattingQueue[0]);
            chattingQueue.RemoveAt(0);
        }
        chattingQueue.Add(newPrefab);
    }

}