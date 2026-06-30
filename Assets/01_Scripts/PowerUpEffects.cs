using UnityEngine;

/// <summary>
/// 데미지 증가 PowerUp
/// 매 선택 시 +5 데미지 누적
/// </summary>
public class DamageUpPowerUp : ScriptableObject, IPowerUpEffect
{
    private const int DAMAGE_INCREASE = 5;

    public void Apply(PlayerController target)
    {
        var shoot = target.GetComponent<PlayerShoot>();
        if (shoot != null && shoot.Stats != null)
        {
            // GunData는 직접 수정할 수 없으므로 (SO는 레퍼런스)
            // PlayerShoot 또는 별도 ModifiedStats 저장소 필요
            // 임시: PlayerShoot에 DamageMultiplier 추가하여 관리
            shoot.ApplyDamageMultiplier(DAMAGE_INCREASE);
            Debug.Log($"[PowerUp] {target.PlayerName} - Damage +{DAMAGE_INCREASE}");
        }
    }
}

/// <summary>
/// 체력 증가 PowerUp
/// 매 선택 시 +50 체력 누적
/// </summary>
public class HealthUpPowerUp : ScriptableObject, IPowerUpEffect
{
    private const int HEALTH_INCREASE = 50;

    public void Apply(PlayerController target)
    {
        target.IncreaseMaxHealth(HEALTH_INCREASE);
        Debug.Log($"[PowerUp] {target.PlayerName} - Health +{HEALTH_INCREASE}");
    }
}

/// <summary>
/// 발사 속도 증가 PowerUp
/// 매 선택 시 fireRate 1.5배 적용
/// </summary>
public class FireRateUpPowerUp : ScriptableObject, IPowerUpEffect
{
    private const float FIRE_RATE_MULTIPLIER = 1.5f;

    public void Apply(PlayerController target)
    {
        var shoot = target.GetComponent<PlayerShoot>();
        if (shoot != null)
        {
            shoot.ApplyFireRateMultiplier(FIRE_RATE_MULTIPLIER);
            Debug.Log($"[PowerUp] {target.PlayerName} - FireRate x{FIRE_RATE_MULTIPLIER}");
        }
    }
}

/// <summary>
/// 이동 속도 증가 PowerUp
/// 매 선택 시 +1.5 이동 속도 누적
/// </summary>
public class SpeedUpPowerUp : ScriptableObject, IPowerUpEffect
{
    private const float SPEED_INCREASE = 1.5f;

    public void Apply(PlayerController target)
    {
        target.IncreaseSpeed(SPEED_INCREASE);
        Debug.Log($"[PowerUp] {target.PlayerName} - Speed +{SPEED_INCREASE}");
    }
}
