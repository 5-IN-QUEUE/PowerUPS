using Fusion;
using UnityEngine;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    public static event System.Action<string, string> OnRoundEnd;

    [Networked] private TickTimer RestartTimer  { get; set; }
    [Networked] private bool      IsRoundEnding { get; set; }
    [Networked] private int SimultaneousDeathCount { get; set; }
    [Networked] private PlayerRef LastKiller { get; set; }

    private static readonly Vector3[] SpawnPoints =
    {
        new Vector3(-4f, 8f,  0f),
        new Vector3( 4f, 8f,  0f),
        new Vector3( 0f, 8f, -4f),
        new Vector3( 0f, 8f,  4f),
    };

    public override void Spawned()
    {
        Instance = this;
    }

    // StateAuthority(서버)에서만 호출됨
    public void RegisterKill(PlayerRef killer)
    {
        if (!HasStateAuthority || IsRoundEnding) return;

        // 동시 사망 감지
        if (SimultaneousDeathCount > 0)
        {
            SimultaneousDeathCount++;
            Debug.Log($"[RoundManager] Simultaneous death detected. Count: {SimultaneousDeathCount}");
            return;
        }

        // 킬러 점수 증가 + 이름 수집
        string killerName = "Unknown";
        if (Runner.TryGetPlayerObject(killer, out var killerObj))
        {
            var ctrl = killerObj.GetComponent<PlayerController>();
            if (ctrl != null)
            {
                ctrl.Score++;
                killerName = ctrl.PlayerName.ToString();
            }
        }

        // 점수 요약 문자열 빌드 (서버가 가진 최신 값)
        var sb = new System.Text.StringBuilder();
        foreach (var p in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(p, out var obj)) continue;
            var c = obj.GetComponent<PlayerController>();
            if (c != null) sb.Append($"{c.PlayerName}:{c.Score}  ");
        }

        IsRoundEnding = true;
        RestartTimer  = TickTimer.CreateFromSeconds(Runner, 3f);
        LastKiller = killer;
        SimultaneousDeathCount = 1;

        RPC_RoundEnd(killerName, sb.ToString());
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !IsRoundEnding) return;
        if (!RestartTimer.Expired(Runner)) return;

        IsRoundEnding = false;

        // 동시 사망 여부에 따라 처리
        if (SimultaneousDeathCount > 1)
        {
            Debug.Log("[RoundManager] Simultaneous death → Both players respawn");
            RespawnBothPlayers();
        }
        else
        {
            Debug.Log($"[RoundManager] Regular kill by {LastKiller}");
            RespawnDeadPlayers();
        }

        SimultaneousDeathCount = 0;

        // GameFlowManager에 상태 전환 알림
        var gfm = FindObjectOfType<GameFlowManager>();
        if (gfm != null && gfm.HasStateAuthority)
        {
            gfm.EvaluateMatchStatus();
        }
    }

    private void RespawnBothPlayers()
    {
        int i = 0;
        foreach (var p in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(p, out var obj)) { i++; continue; }
            var ctrl = obj.GetComponent<PlayerController>();
            if (ctrl == null) { i++; continue; }

            Vector3 pos = i < SpawnPoints.Length
                ? SpawnPoints[i]
                : new Vector3(i * 3f, 8f, 0f);

            ctrl.Respawn(pos);
            i++;
        }
    }

    private void RespawnDeadPlayers()
    {
        int i = 0;
        foreach (var p in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(p, out var obj)) { i++; continue; }
            var ctrl = obj.GetComponent<PlayerController>();
            if (ctrl == null) { i++; continue; }

            if (ctrl.PlayerHealth <= 0)
            {
                Vector3 pos = i < SpawnPoints.Length
                    ? SpawnPoints[i]
                    : new Vector3(i * 3f, 8f, 0f);
                ctrl.Respawn(pos);
            }

            i++;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_RoundEnd(NetworkString<_32> killerName, NetworkString<_64> scores)
    {
        OnRoundEnd?.Invoke(killerName.ToString(), scores.ToString());
    }
}
