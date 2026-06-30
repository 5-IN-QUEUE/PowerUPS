using UnityEngine;

[CreateAssetMenu(menuName = "Game/PowerUpData", fileName = "NewPowerUp")]
public class PowerUpData : ScriptableObject
{
    public string powerUpName;
    public string description;
    public Sprite icon;

    [Header("Effects (0 = 적용 안 함)")]
    public int   damageIncrease;            // 데미지 증가 (flat)
    public int   healthIncrease;            // 최대 체력 증가 (flat)
    public float fireRateMultiplier = 1f;   // 발사속도 배율 (1 = 변화없음)
    public float speedIncrease;             // 이동속도 증가 (flat)
    public int   pelletIncrease;                    // 펠릿 수 증가
    public int   ricochetIncrease;                  // 벽 튕기기 횟수 증가
    public float accuracyChange  = 0.05f;           // 탄퍼짐 변화 (양수=퍼짐↑, 음수=퍼짐↓)
    public float bulletSpeedMultiplier = 1f;        // 탄속 배율 (1=변화없음, 1.5=+50%)

    public void Apply(PlayerController target)
    {
        var shoot = target.GetComponent<PlayerShoot>();

        if (damageIncrease != 0 && shoot != null)
            shoot.ApplyDamageMultiplier(damageIncrease);

        if (healthIncrease != 0)
            target.IncreaseMaxHealth(healthIncrease);

        if (fireRateMultiplier != 1f && shoot != null)
            shoot.ApplyFireRateMultiplier(fireRateMultiplier);

        if (speedIncrease != 0f)
            target.IncreaseSpeed(speedIncrease);

        if (pelletIncrease != 0 && shoot != null)
            shoot.IncreasePellets(pelletIncrease);

        if (ricochetIncrease != 0 && shoot != null)
            shoot.IncreaseRicochets(ricochetIncrease);

        if (accuracyChange != 0f && shoot != null)
            shoot.ChangeAccuracy(accuracyChange);

        if (bulletSpeedMultiplier != 1f && shoot != null)
            shoot.ApplyBulletSpeedMultiplier(bulletSpeedMultiplier);
    }
}
