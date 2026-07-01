using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkInputHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private bool _firePressed  = false;
    private bool _jumpPressed  = false;
    private bool _reloadPressed = false;

    public static event System.Action OnFireInput;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))       _firePressed   = true;
        if (Input.GetKeyDown(KeyCode.Space))   _jumpPressed   = true;
        if (Input.GetKeyDown(KeyCode.R))       _reloadPressed = true;
    }
    // NetworkInputHandler.cs
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        // 채팅창 또는 카드 선택 중이면 이동/사격 입력 무시
        if ((ChattingScript.Instance != null && ChattingScript.Instance.IsChatOpen)
            || UpgradeCardSelect.IsSelecting)
        {
            input.Set(data);
            return;
        }

        // 이하 기존 입력 처리
        if (Input.GetKey(KeyCode.W)) data.direction += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) data.direction += Vector3.back;
        if (Input.GetKey(KeyCode.A)) data.direction += Vector3.left;
        if (Input.GetKey(KeyCode.D)) data.direction += Vector3.right;

        if (CameraController.Instance != null)
        {
            data.rotationY = CameraController.Instance.Yaw;
            data.rotationX = CameraController.Instance.Pitch;
        }

        data.buttons.Set(NetworkInputData.MOUSEBUTTON0, Input.GetMouseButton(0));
        data.buttons.Set(NetworkInputData.JUMP,         _jumpPressed);
        data.buttons.Set(NetworkInputData.RELOAD,       _reloadPressed);

        if (_firePressed) OnFireInput?.Invoke();

        _firePressed   = false;
        _jumpPressed   = false;
        _reloadPressed = false;

        input.Set(data);
    }

    // --- 빈 콜백들 ---
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
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
}