using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 방 목록의 항목 하나에 붙이는 프리팹용 컴포넌트.
// 프리팹을 직접 디자인하고, 방 이름/인원수 텍스트와 클릭용 Button을 인스펙터에 연결해서 사용.
public class RoomListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button joinButton;

    public void Setup(string roomName, int playerCount, int maxPlayers, Action<string> onClick)
    {
        if (roomNameText != null) roomNameText.text = roomName;
        if (playerCountText != null) playerCountText.text = $"{playerCount}/{maxPlayers}";

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => onClick(roomName));
    }
}
