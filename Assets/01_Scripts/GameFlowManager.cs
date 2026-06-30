using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : NetworkBehaviour
{
    public enum GameState
    {
        Waiting,
        Loading,
        AugmentSelect,
        RoundActive,
        RoundEnd,
        MatchEnd
    }

    public static GameFlowManager Instance { get; private set; }

    public static event System.Action<GameState> OnStateChanged;

    [Networked] public GameState CurrentState { get; set; }
    [Networked] private TickTimer SelectTimer { get; set; }
    [Networked] private bool SelectTimeoutOccurred { get; set; }

    private const float AUGMENT_SELECT_DURATION = 30f;

    private void OnEnable()
    {
        RoundManager.OnRoundEnd += HandleRoundEnd;
    }

    private void OnDisable()
    {
        RoundManager.OnRoundEnd -= HandleRoundEnd;
    }

    public override void Spawned()
    {
        Instance = this;
        if (HasStateAuthority)
        {
            CurrentState = GameState.Waiting;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        switch (CurrentState)
        {
            case GameState.Waiting:
                // MatchManager에서 StartMatch() 호출 시 Loading으로 전환
                break;

            case GameState.Loading:
                // 씬 로드 + 스폰 완료 후 AugmentSelect로 자동 전환
                // (PlayerSpawner.OnSceneLoadDone 또는 다른 콜백에서 호출)
                break;

            case GameState.AugmentSelect:
                // 30초 타이머 진행
                if (!SelectTimer.IsRunning)
                {
                    SelectTimer = TickTimer.CreateFromSeconds(Runner, AUGMENT_SELECT_DURATION);
                }
                
                if (SelectTimer.Expired(Runner))
                {
                    // 타이머 만료 시 미선택 플레이어 자동 선택
                    SelectTimeoutOccurred = true;
                    // 실제 자동 선택 로직은 PowerUpManager에서 처리
                }
                break;

            case GameState.RoundActive:
                // RoundManager가 킬을 감지하면 RoundEnd로 전환
                break;

            case GameState.RoundEnd:
                // HandleRoundEnd에서 3초 후 다음 상태 결정
                break;

            case GameState.MatchEnd:
                // UIManager가 최종 결과 표시
                break;
        }
    }

    /// <summary>
    /// MatchManager에서 2인 매칭 완료 시 호출
    /// </summary>
    public void StartMatch()
    {
        if (!HasStateAuthority) return;
        ChangeState(GameState.Loading);
    }

    /// <summary>
    /// 씬 로드 완료 후 호출 (NetworkLauncher 또는 PlayerSpawner에서)
    /// </summary>
    public void OnSceneLoadComplete()
    {
        if (!HasStateAuthority) return;
        ChangeState(GameState.AugmentSelect);
    }

    /// <summary>
    /// AugmentSelect에서 양 플레이어가 선택 완료 시 호출 (PowerUpManager에서)
    /// </summary>
    public void OnAugmentSelectComplete()
    {
        if (!HasStateAuthority) return;
        SelectTimer = default;
        ChangeState(GameState.RoundActive);
    }

    /// <summary>
    /// RoundManager에서 플레이어 사망 감지 시 호출
    /// </summary>
    private void HandleRoundEnd(string killerName, string scores)
    {
        if (!HasStateAuthority) return;
        ChangeState(GameState.RoundEnd);
        // 3초 후 RoundManager.RespawnAll() 직후 EvaluateMatchStatus() 가 호출됨
    }

    /// <summary>
    /// RoundEnd 상태에서 5점 도달 여부를 확인하여 다음 상태 결정
    /// RoundManager.RespawnAll() 직후 호출되어야 함
    /// </summary>
    public void EvaluateMatchStatus()
    {
        if (!HasStateAuthority) return;

        // 플레이어 점수 확인
        int maxScore = 0;
        foreach (var p in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(p, out var obj)) continue;
            var ctrl = obj.GetComponent<PlayerController>();
            if (ctrl != null && ctrl.Score > maxScore)
                maxScore = ctrl.Score;
        }

        if (maxScore >= 5)
        {
            ChangeState(GameState.MatchEnd);
        }
        else
        {
            ChangeState(GameState.AugmentSelect);
        }
    }

    private void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        GameState oldState = CurrentState;
        CurrentState = newState;

        Debug.Log($"[GameFlowManager] State: {oldState} → {newState}");
        RPC_NotifyStateChange(newState);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyStateChange(GameState newState)
    {
        OnStateChanged?.Invoke(newState);
    }
}
