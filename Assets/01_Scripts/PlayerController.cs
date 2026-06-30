using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    private NetworkCharacterController _ncc;
    private TextMesh _nameTag;
    private ChangeDetector _changeDetector;

    public static GameObject localPlayer;

    [SerializeField] private float moveSpeed = 5f;

    [Networked] public Color              PlayerColor  { get; set; }
    [Networked] public int                PlayerHealth { get; set; }
    [Networked] public int                Score        { get; set; }
    [Networked] public NetworkString<_32> PlayerName   { get; set; }
    
    // PowerUp 시스템 추가
    [Networked] private float DamageMultiplier { get; set; }
    [Networked] private float FireRateMultiplier { get; set; }
    [Networked] private float SpeedMultiplier { get; set; }
    [Networked] private int MaxHealthPoints { get; set; }
    [Networked] private int Pellets { get; set; }

    private void Awake()
    {
        _ncc     = GetComponent<NetworkCharacterController>();
        _nameTag = transform.GetChild(0).GetComponent<TextMesh>();
        Pellets = 1;
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasStateAuthority)
        {
            PlayerColor = new Color(
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                Random.Range(0f, 1f)
            );
            PlayerHealth = 150;
            MaxHealthPoints = 150;
            Score        = 0;
            PlayerName   = $"Player{Random.Range(0, 999999)}";
            
            // PowerUp 초기값
            DamageMultiplier = 1f;
            FireRateMultiplier = 1f;
            SpeedMultiplier = 1f;
        }

        if (HasInputAuthority)
            localPlayer = gameObject;

        ApplyPlayerName();
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(PlayerName):  ApplyPlayerName();  break;
                case nameof(PlayerColor): ApplyPlayerColor(); break;
            }
        }
    }

    private void ApplyPlayerName()
    {
        var n = PlayerName.ToString();
        _nameTag.text   = n;
        gameObject.name = n;
    }

    private void ApplyPlayerColor()
    {
        var r = GetComponent<Renderer>();
        if (r != null) r.material.color = PlayerColor;
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData data)) return;

        var rot = Quaternion.Euler(0, data.rotationY, 0);

        Vector3 moveDir = rot * data.direction.normalized;
        _ncc.Move(moveDir * moveSpeed * SpeedMultiplier * Runner.DeltaTime);

        transform.rotation = rot;

        if (data.buttons.IsSet(NetworkInputData.JUMP))
            _ncc.Jump();
    }

    // ==================== 데미지 / 리스폰 ====================

    public void TakeDamage(int amount, PlayerRef killer = default)
    {
        if (!HasStateAuthority) return;

        PlayerHealth = Mathf.Max(0, PlayerHealth - amount);

        if (PlayerHealth <= 0)
        {
            if (RoundManager.Instance != null) RoundManager.Instance.RegisterKill(killer);
            RPC_OnDeath();
        }
    }

    public void Respawn(Vector3 pos)
    {
        if (!HasStateAuthority) return;

        PlayerHealth = MaxHealthPoints;
        _ncc.Teleport(pos);

        var shoot = GetComponent<PlayerShoot>();
        if (shoot != null) shoot.ResetGun();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnDeath()
    {
        Debug.Log($"{PlayerName} 사망");
    }

    // ==================== PowerUp 효과 메서드 ====================

    public void IncreaseMaxHealth(int amount)
    {
        if (!HasStateAuthority) return;
        
        MaxHealthPoints += amount;
        if (PlayerHealth > MaxHealthPoints)
            PlayerHealth = MaxHealthPoints;
        
        Debug.Log($"[PowerUp] Max health increased to {MaxHealthPoints}");
    }
    public void IncreasePellets(int amount){
        if (!HasStateAuthority) return;
        
        Pellets += amount;
        Debug.Log($"[PowerUp] Speed multiplier: {SpeedMultiplier}");
    }
    public void IncreaseSpeed(float amount)
    {
        if (!HasStateAuthority) return;
        
        SpeedMultiplier += (amount / 5f);
        Debug.Log($"[PowerUp] Speed multiplier: {SpeedMultiplier}");
    }

    public void ApplyDamageBoost(int flatIncrease)
    {
        if (!HasStateAuthority) return;
        
        var shoot = GetComponent<PlayerShoot>();
        if (shoot != null)
        {
            shoot.ApplyDamageMultiplier(flatIncrease);
        }
    }

    public void ResetScore()
    {
        if (!HasStateAuthority) return;
        Score = 0;
        Debug.Log($"[PlayerController] {PlayerName} score reset to 0");
    }

    public float GetSpeedMultiplier() => SpeedMultiplier;
    public float GetFireRateMultiplier() => FireRateMultiplier;
    public int GetMaxHealth() => MaxHealthPoints;

    // ==================== 채팅 ====================

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SendChatMessage(string senderName, string message)
    {
        ChattingScript.Instance?.AddChatMessage(senderName, message);
    }
}
