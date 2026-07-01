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

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SubmitCardSelection(PlayerRef sender, int selectedIndex)
    {
        if (!HasStateAuthority) return;

        List<PlayerRef> activePlayers = new List<PlayerRef>(Runner.ActivePlayers);
        if (activePlayers.Count < 2) return;

        if (activePlayers[0] == sender)
        {
            Player0Selected = true;
            Player0SelectedIndex = selectedIndex;
            Debug.Log($"[PowerUpManager] Player 0 selected: {selectedIndex}");
        }
        else if (activePlayers[1] == sender)
        {
            Player1Selected = true;
            Player1SelectedIndex = selectedIndex;
            Debug.Log($"[PowerUpManager] Player 1 selected: {selectedIndex}");
        }

        // 양 플레이어가 모두 선택 완료 시
        if (Player0Selected && Player1Selected)
        {
            ApplyPowerUps(activePlayers[0], activePlayers[1]);
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

        if (!Player0Selected)
        {
            Player0SelectedIndex = Random.Range(0, powerUpCandidates.Length);
            Player0Selected = true;
            Debug.Log($"[PowerUpManager] Player 0 auto-selected: {Player0SelectedIndex}");
        }

        if (!Player1Selected)
        {
            Player1SelectedIndex = Random.Range(0, powerUpCandidates.Length);
            Player1Selected = true;
            Debug.Log($"[PowerUpManager] Player 1 auto-selected: {Player1SelectedIndex}");
        }

        ApplyPowerUps(activePlayers[0], activePlayers[1]);
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
