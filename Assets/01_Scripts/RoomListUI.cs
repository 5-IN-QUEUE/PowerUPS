using System.Collections.Generic;
using Fusion;
using UnityEngine;

// kawaii_creatureScene의 방 목록 패널(ScrollView Content)에 붙여서 사용.
// NetworkLauncher가 로비에서 받아온 방 목록을 RoomListItem 프리팹으로 한 줄씩 그려준다.
public class RoomListUI : MonoBehaviour
{
    [SerializeField] private RoomListItem roomListItemPrefab;
    [SerializeField] private Transform content;

    private readonly List<RoomListItem> _items = new List<RoomListItem>();

    // 탭을 닫았다 다시 여는 식으로 이 패널이 SetActive(false/true) 되는 UI일 수 있으므로
    // Start/OnDestroy 대신 OnEnable/OnDisable을 써서 다시 열릴 때마다 최신 목록을 즉시 그린다.
    private void OnEnable()
    {
        if (NetworkLauncher.Instance == null) return;

        NetworkLauncher.Instance.OnRoomListUpdated += HandleRoomListUpdated;
        NetworkLauncher.Instance.OnMatchStateChanged += HandleMatchStateChanged;

        // 구독하기 전에 이미 와있던 목록(캐시)을 즉시 반영 - 안 그러면 변경 이벤트가 다시 안 올 때까지 빈 목록으로 남는다.
        HandleRoomListUpdated(NetworkLauncher.Instance.CurrentSessionList);
    }

    private void OnDisable()
    {
        if (NetworkLauncher.Instance == null) return;
        NetworkLauncher.Instance.OnRoomListUpdated -= HandleRoomListUpdated;
        NetworkLauncher.Instance.OnMatchStateChanged -= HandleMatchStateChanged;
        Clear();
    }

    private void HandleMatchStateChanged(NetworkLauncher.MatchState state)
    {
        // 방을 찾는 중이거나 입장한 뒤에는 로비 목록 갱신이 멈추므로 비워둔다
        if (state != NetworkLauncher.MatchState.Idle) Clear();
    }

    private void HandleRoomListUpdated(List<SessionInfo> sessions)
    {
        Clear();

        foreach (var session in sessions)
        {
            if (!session.IsOpen || !session.IsVisible) continue;
            if (session.PlayerCount >= session.MaxPlayers) continue;

            var item = Instantiate(roomListItemPrefab, content);
            item.Setup(session.Name, session.PlayerCount, session.MaxPlayers, OnRoomClicked);
            _items.Add(item);
        }
    }

    private void OnRoomClicked(string roomName)
    {
        NetworkLauncher.Instance.JoinOrCreateRoom(roomName);
    }

    private void Clear()
    {
        foreach (var item in _items)
        {
            if (item != null) Destroy(item.gameObject);
        }
        _items.Clear();
    }
}
