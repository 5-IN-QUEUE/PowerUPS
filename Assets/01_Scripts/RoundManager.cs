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
    [Networked] private bool PendingBulletCleanup { get; set; }

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
        if (!HasStateAuthority) return;

        // 이미 라운드 종료 처리 중(누군가 먼저 죽어서 타이머가 도는 중)인데
        // 또 다른 플레이어의 사망이 들어오면 동시 사망으로 처리한다.
        // 예전 코드는 위쪽에서 IsRoundEnding이면 무조건 return 해버려서
        // 이 분기가 절대 실행되지 않는 죽은 코드였다 — 그래서 맞대결로 동시에
        // 죽어도 항상 먼저 처리된 한쪽만 "패자"로 기록되고, 두 번째로 죽은
        // 플레이어는 다음 라운드 증강 선택에서 제외되던 버그의 원인이었다.
        if (IsRoundEnding)
        {
            if (victim != LastVictim)
            {
                LastVictim = PlayerRef.None; // 특정할 수 없는 패자 → 다음 카드 선택은 둘 다 진행
                SimultaneousDeathCount++;
                Debug.Log($"[RoundManager] Simultaneous death detected. Count: {SimultaneousDeathCount}");
            }
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

        // 라운드 종료 순간 날아다니던 총알(같은 산탄에서 나온 나머지 펠릿 등)이
        // 리스폰 대기 중인 플레이어에게 뒤늦게 명중해 체력을 깎거나, 죽은 걸로
        // 잘못 재판정되는 걸 막는다. (리스폰 직후 체력이 max로 안 돌아오는
        // 것처럼 보이던 문제의 원인 중 하나)
        //
        // 여기서 바로 DespawnAllBullets()를 호출하면 안 된다: RegisterKill은
        // "명중한 그 총알"의 BulletScript.FixedUpdateNetwork() 안에서 호출되는데,
        // 그 총알 자신까지 여기서 despawn시켜버리면 원래 코드가 되돌아가서
        // Shooter 프로퍼티를 다시 읽으려 할 때 이미 파괴된 네트워크 오브젝트라
        // InvalidOperationException이 터진다. 플래그만 세워 다음 틱(RoundManager
        // 자신의 FixedUpdateNetwork)에서 정리해 재진입 문제를 피한다.
        PendingBulletCleanup = true;

        RPC_RoundEnd(killerName, sb.ToString());
    }

    private void DespawnAllBullets()
    {
        foreach (var bullet in FindObjectsOfType<BulletScript>())
        {
            if (bullet.Object != null && bullet.Object.IsValid)
                Runner.Despawn(bullet.Object);
        }
    }

    /// <summary>
    /// 이번 라운드에 해당 플레이어가 증강 카드를 선택할 자격이 있는지.
    /// LastVictim이 Networked public이라 모든 클라이언트가 직접 읽을 수 있어
    /// UI 쪽(승자에게는 카드 화면을 아예 띄우지 않는 용도)에서 사용한다.
    /// </summary>
    public bool IsEligibleThisRound(PlayerRef p)
    {
        return LastVictim == PlayerRef.None || p == LastVictim;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (PendingBulletCleanup)
        {
            PendingBulletCleanup = false;
            DespawnAllBullets();
        }

        if (!IsRoundEnding) return;
        if (!RestartTimer.Expired(Runner)) return;

        IsRoundEnding = false;

        // 승패와 상관없이 라운드가 끝나면 두 플레이어 모두 스폰 위치로 복귀,
        // 체력/탄약을 초기화한다 (파워업으로 얻은 스탯은 유지됨).
        RespawnBothPlayers();

        SimultaneousDeathCount = 0;

        // GameFlowManager에 상태 전환 알림
        var gfm = GameFlowManager.Instance;
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
