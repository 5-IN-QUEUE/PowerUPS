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

    // 이번 라운드에서 죽은(진) 플레이어. PowerUpManager가 "패자만 카드 선택" 판단에 사용한다.
    // 동시 사망이거나 아직 아무도 죽지 않은 첫 라운드에는 PlayerRef.None으로 둬서
    // "둘 다 선택" 케이스임을 나타낸다.
    [Networked] public PlayerRef LastVictim { get; private set; }

    private static readonly Vector3[] FallbackSpawnPoints =
    {
        new Vector3(-3f, 1f, 0f),
        new Vector3( 3f, 1f, 0f),
        new Vector3(-3f, 1f, 3f),
        new Vector3( 3f, 1f, 3f),
    };

    private Vector3 GetRespawnPosition(int index)
    {
        var point = GameObject.Find("SpawnPoint_" + index);
        if (point != null) return point.transform.position;
        return index < FallbackSpawnPoints.Length
            ? FallbackSpawnPoints[index]
            : new Vector3(index * 3f, 1f, 0f);
    }

    public override void Spawned()
    {
        Instance = this;
    }

    // StateAuthority(서버)에서만 호출됨
    public void RegisterKill(PlayerRef killer, PlayerRef victim)
    {
        if (!HasStateAuthority || IsRoundEnding) return;

        // 동시 사망 감지
        if (SimultaneousDeathCount > 0)
        {
            SimultaneousDeathCount++;
            LastVictim = PlayerRef.None; // 특정할 수 없는 패자 → 다음 카드 선택은 둘 다 진행
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
        LastVictim = victim;
        SimultaneousDeathCount = 1;

        RPC_RoundEnd(killerName, sb.ToString());
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !IsRoundEnding) return;
        if (!RestartTimer.Expired(Runner)) return;

        IsRoundEnding = false;

        // 승패와 상관없이 라운드가 끝나면 두 플레이어 모두 스폰 위치로 복귀,
        // 체력/탄약을 초기화한다 (파워업으로 얻은 스탯은 유지됨).
        RespawnBothPlayers();

        SimultaneousDeathCount = 0;

        // GameFlowManager에 상태 전환 알림 (비활성 포함 검색)
        var gfm = FindObjectOfType<GameFlowManager>(true);
        if (gfm != null && gfm.HasStateAuthority)
        {
            gfm.EvaluateMatchStatus();
        }
        else if (gfm == null)
        {
            Debug.LogError("[RoundManager] GameFlowManager를 찾을 수 없습니다!");
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

            ctrl.Respawn(GetRespawnPosition(i));
            i++;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_RoundEnd(NetworkString<_32> killerName, NetworkString<_64> scores)
    {
        OnRoundEnd?.Invoke(killerName.ToString(), scores.ToString());
    }
}
