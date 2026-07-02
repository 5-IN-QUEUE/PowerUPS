using Fusion;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// PowerUp 선택 및 적용을 관리하는 NetworkBehaviour
/// StateAuthority 권한으로 효과 적용 및 RPC 브로드캐스트
/// </summary>
public class PowerUpManager : NetworkBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    public static event System.Action<PlayerRef, string, string> OnPowerUpApplied; // player, powerUpName, description

    // PowerUp 카드 후보 (Inspector에서 4개 할당)
    [SerializeField] private PowerUpData[] powerUpCandidates = new PowerUpData[4];

    // 슬롯(0/1)과 실제 PlayerRef의 매핑. Runner.ActivePlayers의 열거 순서는
    // 호출마다 같다는 보장이 없으므로, 한 번 배정된 슬롯은 매치 내내 고정한다.
    [Networked] private PlayerRef Player0Ref { get; set; }
    [Networked] private PlayerRef Player1Ref { get; set; }
    [Networked] private bool Player0RefAssigned { get; set; }
    [Networked] private bool Player1RefAssigned { get; set; }

    // 각 플레이어의 선택 상태
    [Networked] private bool Player0Selected { get; set; }
    [Networked] private bool Player1Selected { get; set; }
    [Networked] private int Player0SelectedIndex { get; set; } = -1;
    [Networked] private int Player1SelectedIndex { get; set; } = -1;

    public override void Spawned()
    {
        Instance = this;
    }

    /// <summary>
    /// UpgradeCardSelect에서 카드 확정 시 호출 (로컬 플레이어 기준)
    /// </summary>
    public void OnCardConfirmed(int selectedIndex)
    {
        if (Runner == null) return;
        // 씬 NetworkObject는 InputAuthority가 없으므로 발신자 PlayerRef를 직접 넘긴다
        RPC_SubmitCardSelection(Runner.LocalPlayer, selectedIndex);
    }

    /// <summary>
    /// sender를 슬롯 0 또는 1에 고정 배정하고 슬롯 번호를 반환한다.
    /// 이미 배정된 sender면 기존 슬롯을, 처음 보는 sender면 비어있는 슬롯을 배정한다.
    /// </summary>
    private int ResolveSlot(PlayerRef p)
    {
        if (Player0RefAssigned && Player0Ref == p) return 0;
        if (Player1RefAssigned && Player1Ref == p) return 1;

        if (!Player0RefAssigned)
        {
            Player0Ref = p;
            Player0RefAssigned = true;
            return 0;
        }
        if (!Player1RefAssigned)
        {
            Player1Ref = p;
            Player1RefAssigned = true;
            return 1;
        }

        return -1; // 두 슬롯이 이미 다른 플레이어로 채워진 상태 (정상 2인 대전에서는 발생하지 않음)
    }

    /// <summary>
    /// 이번 라운드에 두 플레이어 모두 카드를 선택해야 하는지 여부.
    /// 아직 아무도 죽지 않은 첫 라운드, 혹은 동시 사망이었던 라운드에는 둘 다 선택한다.
    /// 그 외에는 라운드에서 진(죽은) 플레이어만 선택한다.
    /// </summary>
    private bool BothMustSelect()
    {
        return RoundManager.Instance == null || RoundManager.Instance.LastVictim == PlayerRef.None;
    }

    private bool IsEligible(PlayerRef p)
    {
        return BothMustSelect() || p == RoundManager.Instance.LastVictim;
    }

    private bool SelectionsComplete()
    {
        if (BothMustSelect())
            return Player0Selected && Player1Selected;

        PlayerRef victim = RoundManager.Instance.LastVictim;
        if (Player0RefAssigned && Player0Ref == victim) return Player0Selected;
        if (Player1RefAssigned && Player1Ref == victim) return Player1Selected;
        return false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SubmitCardSelection(PlayerRef sender, int selectedIndex)
    {
        if (!HasStateAuthority) return;

        if (!IsEligible(sender))
        {
            Debug.LogWarning($"[PowerUpManager] {sender}는 이번 라운드의 승자라 카드를 선택할 수 없습니다. 선택을 무시합니다.");
            return;
        }

        int slot = ResolveSlot(sender);
        if (slot == 0)
        {
            Player0Selected = true;
            Player0SelectedIndex = selectedIndex;
            Debug.Log($"[PowerUpManager] Player0({sender}) selected: {selectedIndex}");
        }
        else if (slot == 1)
        {
            Player1Selected = true;
            Player1SelectedIndex = selectedIndex;
            Debug.Log($"[PowerUpManager] Player1({sender}) selected: {selectedIndex}");
        }
        else
        {
            Debug.LogWarning($"[PowerUpManager] {sender}에 배정할 슬롯이 없습니다. 선택을 무시합니다.");
            return;
        }

        if (SelectionsComplete())
        {
            ApplyPowerUps(Player0Ref, Player1Ref);
            ResetSelections();
        }
    }

    /// <summary>
    /// 타이머 만료 시 미선택 플레이어 자동 선택 (GameFlowManager에서 호출)
    /// </summary>
    public void HandleSelectionTimeout()
    {
        if (!HasStateAuthority) return;

        List<PlayerRef> activePlayers = new List<PlayerRef>(Runner.ActivePlayers);
        if (activePlayers.Count < 2) return;

        // 지금까지 한 번도 선택을 보내지 않은 플레이어도 슬롯에 배정해둔다.
        foreach (var p in activePlayers)
            ResolveSlot(p);

        if (!Player0Selected && IsEligible(Player0Ref))
        {
            Player0SelectedIndex = Random.Range(0, powerUpCandidates.Length);
            Player0Selected = true;
            Debug.Log($"[PowerUpManager] Player0({Player0Ref}) auto-selected: {Player0SelectedIndex}");
        }

        if (!Player1Selected && IsEligible(Player1Ref))
        {
            Player1SelectedIndex = Random.Range(0, powerUpCandidates.Length);
            Player1Selected = true;
            Debug.Log($"[PowerUpManager] Player1({Player1Ref}) auto-selected: {Player1SelectedIndex}");
        }

        ApplyPowerUps(Player0Ref, Player1Ref);
        ResetSelections();
    }

    private void ApplyPowerUps(PlayerRef player0, PlayerRef player1)
    {
        // Player 0 의 선택 효과 적용
        if (Runner.TryGetPlayerObject(player0, out var obj0) && Player0SelectedIndex >= 0)
        {
            var ctrl0 = obj0.GetComponent<PlayerController>();
            var powerUp0 = powerUpCandidates[Player0SelectedIndex];

            if (ctrl0 != null && powerUp0 != null)
            {
                powerUp0.Apply(ctrl0);
                RPC_NotifyPowerUpApplied(player0, powerUp0.powerUpName, powerUp0.description);
            }
        }

        // Player 1 의 선택 효과 적용
        if (Runner.TryGetPlayerObject(player1, out var obj1) && Player1SelectedIndex >= 0)
        {
            var ctrl1 = obj1.GetComponent<PlayerController>();
            var powerUp1 = powerUpCandidates[Player1SelectedIndex];

            if (ctrl1 != null && powerUp1 != null)
            {
                powerUp1.Apply(ctrl1);
                RPC_NotifyPowerUpApplied(player1, powerUp1.powerUpName, powerUp1.description);
            }
        }

        // 양쪽 모두 선택 완료 → GameFlowManager 상태 전환
        GameFlowManager gfm = FindObjectOfType<GameFlowManager>();
        if (gfm != null)
        {
            gfm.OnAugmentSelectComplete();
        }
        else
        {
            Debug.LogWarning("[PowerUpManager] GameFlowManager를 찾지 못해 RoundActive로 전환하지 못했습니다.");
        }
    }

    private void ResetSelections()
    {
        Player0Selected = false;
        Player1Selected = false;
        Player0SelectedIndex = -1;
        Player1SelectedIndex = -1;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyPowerUpApplied(PlayerRef player, NetworkString<_32> powerUpName, NetworkString<_64> description)
    {
        OnPowerUpApplied?.Invoke(player, powerUpName.ToString(), description.ToString());
        Debug.Log($"[PowerUpManager] {powerUpName} applied to {player}");
    }
}
