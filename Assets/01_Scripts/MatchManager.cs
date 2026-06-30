using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MatchManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static MatchManager Instance { get; private set; }
    
    [SerializeField] private GameObject waitingUI; // "상대를 기다리는 중..." UI
    
    private NetworkRunner _runner;
    private bool _matchStarted = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        if (waitingUI != null)
            waitingUI.SetActive(true);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        _runner = runner;
        
        Debug.Log($"[MatchManager] Player joined: {player}, Active players: {runner.ActivePlayers.Count()}");
        
        // 2명 모두 접속 시에만 게임 시작
        if (runner.ActivePlayers.Count() >= 2 && !_matchStarted)
        {
            _matchStarted = true;
            Debug.Log("[MatchManager] 2 players detected. Starting game...");
            
            if (waitingUI != null)
                waitingUI.SetActive(false);
            
            // GameFlowManager에 게임 시작 신호 전송
            GameFlowManager gfm = FindObjectOfType<GameFlowManager>();
            if (gfm != null)
            {
                gfm.StartMatch();
            }
            else
            {
                Debug.LogWarning("[MatchManager] GameFlowManager not found!");
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[MatchManager] Player left: {player}");
        _matchStarted = false;
        
        if (waitingUI != null)
            waitingUI.SetActive(true);
    }

    // 세션 최대 플레이어 수 제한은 NetworkRunner의 GameMode 설정에서 관리
    // 참고: StartGameArgs에서 PlayerCount = 2로 설정 필요

    #region INetworkRunnerCallbacks (빈 구현)
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    #endregion
}
