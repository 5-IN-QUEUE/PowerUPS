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
    [SerializeField] private string _battleScenePath = "Assets/00_Scenes/Play.unity";
    [SerializeField] private int _requiredPlayerCount = 2;
    [SerializeField] private float _matchFoundDelay = 1.5f; // "매칭 완료!" 문구를 보여줄 시간

    private NetworkRunner _runner;
    private bool _lobbyReady = false;

    public MatchState State { get; private set; } = MatchState.Idle;
    public bool IsLobbyReady => _lobbyReady;
    public string CurrentRoomName { get; private set; }
    // OnSessionListUpdated는 목록이 "바뀔 때만" 오는 푸시 이벤트라서, UI가 늦게 구독하면
    // 그 사이의 변경을 영영 놓친다. 마지막으로 받은 목록을 캐싱해뒀다가 구독 시점에 즉시 보여준다.
    public List<SessionInfo> CurrentSessionList { get; private set; } = new List<SessionInfo>();
    public event Action<MatchState> OnMatchStateChanged;
    public event Action<int, int> OnPlayerCountChanged; // (현재 인원, 필요 인원)
    public event Action<List<SessionInfo>> OnRoomListUpdated;
    public event Action<bool> OnLobbyReadyChanged;

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
        _lobbyReady = false;
        CurrentSessionList = new List<SessionInfo>();
        OnLobbyReadyChanged?.Invoke(false);

        _runner = Instantiate(_networkRunnerPrefab);
        _runner.ProvideInput = true;

        _runner.AddCallbacks(this);
        // GetComponent가 null을 반환하면 AddCallbacks(null)이 콜백 리스트에 그대로 들어가고,
        // Fusion이 나중에 그 항목을 호출하는 시점에 NullReferenceException이 터진다.
        // 이 오브젝트에 실제로 붙어있는 컴포넌트만 등록한다.
        AddCallbacksIfPresent<NetworkInputHandler>();
        AddCallbacksIfPresent<PlayerSpawner>();
        AddCallbacksIfPresent<MatchManager>();

        // JoinSessionLobby가 끝나기 전(NameServer -> Master 연결 중)에 StartGame을 보내면
        // "Operation JoinOrCreateRoom not allowed on current server (NameServer)" 에러가 난다.
        // 반드시 이 await가 끝난 뒤에만 방 생성/입장을 허용해야 한다.
        var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);

        if (!result.Ok)
        {
            Debug.LogError($"[NetworkLauncher] 로비 연결 실패: {result.ShutdownReason}");
            return;
        }

        _lobbyReady = true;
        OnLobbyReadyChanged?.Invoke(true);
    }

    private void AddCallbacksIfPresent<T>() where T : Component, INetworkRunnerCallbacks
    {
        if (TryGetComponent<T>(out var callback))
            _runner.AddCallbacks(callback);
        else
            Debug.LogWarning($"[NetworkLauncher] {typeof(T).Name} 컴포넌트가 이 오브젝트에 없어서 콜백 등록을 건너뜁니다.");
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

        if (_runner == null || !_lobbyReady)
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
        CurrentSessionList = sessionList;
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
