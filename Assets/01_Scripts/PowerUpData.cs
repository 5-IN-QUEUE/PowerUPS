using UnityEngine;

[CreateAssetMenu(menuName = "Game/PowerUpData", fileName = "NewPowerUp")]
public class PowerUpData : ScriptableObject
{
    public string       powerUpName;
    public string       description;
    public Sprite       icon;
    public ScriptableObject effectObject; // DamageUpPowerUp / HealthUpPowerUp 등 할당

    // 인터페이스는 Unity가 직렬화 불가 → ScriptableObject로 보관 후 캐스팅
    public IPowerUpEffect Effect => effectObject as IPowerUpEffect;
}
