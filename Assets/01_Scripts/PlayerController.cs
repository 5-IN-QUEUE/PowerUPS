using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    // ==================== 필드 ====================

    private NetworkCharacterController _ncc;
    private TextMesh _nameTag;
    private ChangeDetector _changeDetector;

    public static GameObject localPlayer;

    [SerializeField] private float moveSpeed  = 5f;

    [Networked] public Color              PlayerColor  { get; set; }
    [Networked] public int                PlayerHealth { get; set; }
    [Networked] public NetworkString<_32> PlayerName   { get; set; }

    // ==================== 생명주기 ====================

    private void Awake()
    {
        _ncc     = GetComponent<NetworkCharacterController>();
        _nameTag = transform.GetChild(0).GetComponent<TextMesh>();
    }

    public override void Spawned()
    {
        // ✅ Fusion 2 - ChangeDetector 초기화는 반드시 Spawned()에서
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasStateAuthority)
        {
            PlayerColor  = new Color(
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                Random.Range(0f, 1f)
            );
            PlayerHealth = 150;
            PlayerName   = $"Player{Random.Range(0, 999999)}";
        }

        if (HasInputAuthority)
            localPlayer = gameObject;

        // Spawned 시점엔 이미 값이 있으므로 직접 반영
        ApplyPlayerName();
    }

    // ==================== 변경 감지 (Fusion 2) ====================

    // ✅ Render()는 매 프레임 호출 → 여기서 변경 감지
    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(PlayerName):
                    ApplyPlayerName();
                    break;

                case nameof(PlayerColor):
                    ApplyPlayerColor();
                    break;

                case nameof(PlayerHealth):
                    // HP UI 업데이트 필요 시 여기서
                    // UIManager.Instance?.UpdateHP(PlayerHealth);
                    break;
            }
        }
    }

    private void ApplyPlayerName()
    {
        var name       = PlayerName.ToString();
        _nameTag.text  = name;
        gameObject.name = name;
    }

    private void ApplyPlayerColor()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = PlayerColor;
    }

    // ==================== 입력 처리 ====================

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData data)) return;

        var rot = Quaternion.Euler(0, data.rotationY, 0);

        // 이동 먼저 (NCC가 내부에서 rotation을 건드릴 수 있음)
        Vector3 moveDir = rot * data.direction.normalized;
        _ncc.Move(moveDir * moveSpeed * Runner.DeltaTime);

        // Move 이후에 덮어써서 NCC 자동회전 방지
        transform.rotation = rot;

        // 점프 (NetworkButtons로 누락 없이 처리)
        if (data.buttons.IsSet(NetworkInputData.JUMP))
            _ncc.Jump();
    }

    // ==================== 데미지 처리 ====================

    // 외부(슈팅 등)에서 호출 - StateAuthority에서만 실행됨
    public void TakeDamage(int amount)
    {
        if (!HasStateAuthority) return;

        PlayerHealth = Mathf.Max(0, PlayerHealth - amount);

        if (PlayerHealth <= 0)
            RPC_OnDeath();
    }

    // 사망 처리 - Host가 모든 클라이언트에 알림
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnDeath()
    {
        Debug.Log($"{PlayerName} 사망");
        // TODO: 사망 애니메이션, 리스폰 처리 등
    }

    // ==================== 채팅 ====================

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SendChatMessage(string senderName, string message)
    {
        ChattingScript.Instance?.AddChatMessage(senderName, message);
    }
}