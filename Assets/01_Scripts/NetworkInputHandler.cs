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

        // 채팅창이 열려 있거나, 증강 선택 페이즈(카드 UI를 볼 수 없는 승자 쪽도 포함)면
        // 이동/사격 입력을 무시한다.
        // UpgradeCardSelect.IsSelecting은 "내 화면에 카드 UI가 떠 있는지"만 나타내는
        // 로컬 상태라, 이번 라운드 선택 자격이 없어 카드 패널이 아예 안 뜨는 승자 쪽은
        // 이 값이 false로 남아 입력이 안 막히고 총이 나가는 문제가 있었다.
        // 그래서 양쪽 모두를 동일하게 멈춰야 하는 이 페이즈는 GameFlowManager의
        // 네트워크 상태로 직접 판단한다.
        bool isAugmentSelectPhase = GameFlowManager.Instance != null
            && GameFlowManager.Instance.CurrentState == GameFlowManager.GameState.AugmentSelect;

        if ((ChattingScript.Instance != null && ChattingScript.Instance.IsChatOpen)
            || UpgradeCardSelect.IsSelecting
            || isAugmentSelectPhase)
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