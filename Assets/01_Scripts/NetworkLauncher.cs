using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkLauncher Instance { get; private set; }

    public enum MatchState { Idle, Searching, Found, Failed }

    [SerializeField] private NetworkRunner _networkRunnerPrefab;
    [SerializeField] private string _battleScenePath = "Assets/00_Scenes/JoPockScene.unity";
    [SerializeField] private int _requiredPlayerCount = 2;
    [SerializeField] private float _matchFoundDelay = 1.5f; // "매칭 완료!" 문구를 보여줄 시간

    private NetworkRunner _runner;

    public MatchState State { get; private set; } = MatchState.Idle;
    public string CurrentRoomName { get; private set; }
    public event Action<MatchState> OnMatchStateChanged;
    public event Action<int, int> OnPlayerCountChanged; // (현재 인원, 필요 인원)
    public event Action<List<SessionInfo>> OnRoomListUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ConnectToLobby();
    }

    // ==================== 로비 연결 (방 목록 수신용) ====================

    private async void ConnectToLobby()
    {
        _runner = Instantiate(_networkRunnerPrefab);
        _runner.ProvideInput = true;

        _runner.AddCallbacks(this);
        _runner.AddCallbacks(GetComponent<NetworkInputHandler>());
        _runner.AddCallbacks(GetComponent<PlayerSpawner>());
        _runner.AddCallbacks(GetComponent<MatchManager>());

        await _runner.JoinSessionLobby(SessionLobby.ClientServer);
    }

    // ==================== 방 생성 / 입장 ====================

    // 방 목록에서 클릭하거나, 직접 이름을 입력해서 방을 만들 때 호출.
    // 이미 존재하는 이름이면 입장(클라이언트), 없으면 새로 생성(호스트)된다.
    public async void JoinOrCreateRoom(string roomName)
    {
        if (State == MatchState.Searching) return;

        if (string.IsNullOrWhiteSpace(roomName))
        {
            Debug.LogWarning("[NetworkLauncher] 방 이름을 입력해주세요.");
            return;
        }

        if (_runner == null)
        {
            Debug.LogWarning("[NetworkLauncher] 아직 로비에 연결되지 않았습니다. 잠시 후 다시 시도해주세요.");
            return;
        }

        CurrentRoomName = roomName;
        SetState(MatchState.Searching);
        OnPlayerCountChanged?.Invoke(0, _requiredPlayerCount);

        var sceneManager = GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null)
            sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = roomName, // 같은 이름의 방이 있으면 입장, 없으면 새로 생성
            PlayerCount = _requiredPlayerCount, // 정원 2명 (꽉 차면 더 이상 입장 불가)
            SceneManager = sceneManager
        });

        if (!result.Ok)
        {
            Debug.LogError($"[NetworkLauncher] 방 입장/생성 실패: {result.ShutdownReason}");
            SetState(MatchState.Failed);
        }
    }

    public void CancelMatchmaking()
    {
        if (_runner != null)
        {
            _runner.Shutdown();
            _runner = null;
        }
        SetState(MatchState.Idle);
        ConnectToLobby(); // 방 목록을 계속 받기 위해 로비에 다시 연결
    }

    private void SetState(MatchState state)
    {
        State = state;
        OnMatchStateChanged?.Invoke(state);
    }

    // ==================== 매칭 완료 -> 씬 전환 ====================

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner != _runner) return;

        int current = runner.SessionInfo.PlayerCount;
        OnPlayerCountChanged?.Invoke(current, _requiredPlayerCount);

        if (current >= _requiredPlayerCount && State == MatchState.Searching && runner.IsServer)
        {
            SetState(MatchState.Found);
            StartCoroutine(TransitionToBattleScene());
        }
    }

    private IEnumerator TransitionToBattleScene()
    {
        yield return new WaitForSeconds(_matchFoundDelay);

        int buildIndex = SceneUtility.GetBuildIndexByScenePath(_battleScenePath);
        if (buildIndex < 0)
        {
            Debug.LogError($"[NetworkLauncher] '{_battleScenePath}' 가 Build Settings에 등록되어 있지 않습니다.");
            yield break;
        }

        _runner.LoadScene(SceneRef.FromIndex(buildIndex), LoadSceneMode.Single);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner != _runner) return;
        OnPlayerCountChanged?.Invoke(runner.SessionInfo.PlayerCount, _requiredPlayerCount);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (runner != _runner) return;
        if (State != MatchState.Found && State != MatchState.Idle) SetState(MatchState.Failed);
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        if (runner != _runner) return;
        if (State != MatchState.Found && State != MatchState.Idle) SetState(MatchState.Failed);
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        if (runner != _runner) return;
        SetState(MatchState.Failed);
    }

    // --- 사용하지 않는 콜백들 ---
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (runner != _runner) return;
        OnRoomListUpdated?.Invoke(sessionList);
    }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
