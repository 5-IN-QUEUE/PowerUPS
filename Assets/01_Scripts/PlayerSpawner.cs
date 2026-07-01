using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    [SerializeField] private string _spawnPointNamePrefix = "SpawnPoint_";
    [SerializeField] private string _battleSceneName = "Play";

    // 세션에 참여한 플레이어를 기억해뒀다가 전투 씬 로드 후 재스폰에 사용
    private readonly List<PlayerRef> _registeredPlayers = new List<PlayerRef>();
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private NetworkRunner _runner;

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        _runner = runner;

        if (!_registeredPlayers.Contains(player))
            _registeredPlayers.Add(player);

        // 로비(kawaii_creatureScene)에서는 스폰하지 않는다.
        // LoadSceneMode.Single로 씬 전환 시 로비 씬 오브젝트가 파괴되어
        // PlayerController.localPlayer가 null이 되고 카메라가 공허를 보게 된다.
        if (runner.IsServer && IsBattleScene())
        {
            SpawnPlayer(runner, player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        _registeredPlayers.Remove(player);

        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            if (networkObject != null)
                runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log($"[PlayerSpawner] Scene load done: {SceneManager.GetActiveScene().name}");
        StartCoroutine(SpawnPlayersAndNotifyGameFlow(runner));
    }

    private IEnumerator SpawnPlayersAndNotifyGameFlow(NetworkRunner runner)
    {
        // NetworkBehaviour.Spawned()가 완료되도록 2프레임 대기
        yield return null;
        yield return null;

        if (runner.IsServer && IsBattleScene())
        {
            // 이전 씬에서 스폰된 오브젝트가 파괴됐을 수 있으므로 정리 후 재스폰
            _spawnedCharacters.Clear();

            foreach (var player in _registeredPlayers)
            {
                SpawnPlayer(runner, player);
            }
        }

        // GameFlowManager에 씬 로드 완료 신호 전송 (비활성 포함 검색)
        var gfm = FindObjectOfType<GameFlowManager>(true);
        if (gfm == null)
        {
            Debug.LogError("[PlayerSpawner] GameFlowManager를 씬에서 찾을 수 없습니다! " +
                           "Play 씬에 NetworkObject + GameFlowManager가 붙은 오브젝트를 추가해주세요.");
            yield break;
        }

        Debug.Log($"[PlayerSpawner] GameFlowManager 발견. HasStateAuthority={gfm.HasStateAuthority}, State={gfm.CurrentState}");

        if (gfm.HasStateAuthority)
            gfm.OnSceneLoadComplete();
    }

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.ContainsKey(player)) return;

        Vector3 spawnPosition = GetSpawnPosition(_spawnedCharacters.Count);
        NetworkObject obj = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
        _spawnedCharacters.Add(player, obj);
        Debug.Log($"[PlayerSpawner] Player {player} spawned at {spawnPosition}");
    }

    private bool IsBattleScene()
    {
        return SceneManager.GetActiveScene().name == _battleSceneName;
    }

    private Vector3 GetSpawnPosition(int index)
    {
        var point = GameObject.Find(_spawnPointNamePrefix + index);
        if (point != null) return point.transform.position;
        // 스폰포인트가 없을 때의 기본 위치
        return new Vector3(index == 0 ? -3f : 3f, 1f, 0f);
    }

    public void OnSceneLoadStart(NetworkRunner runner) { }

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
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
