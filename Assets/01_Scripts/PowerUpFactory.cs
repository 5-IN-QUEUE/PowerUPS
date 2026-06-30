using UnityEngine;

/// <summary>
/// PowerUp 에셋 생성 헬퍼 (Unity Editor에서 사용)
/// Assets > Create > Game > PowerUpData로 생성하면 됨
/// </summary>
public class PowerUpFactory
{
    // Editor에서 다음과 같이 생성:
    // 1. Assets/12_ScriptableOBJ/PowerUps 폴더 생성
    // 2. Create > Game > PowerUpData로 4개 생성:
    //    - DamageUp.asset
    //    - HealthUp.asset
    //    - FireRateUp.asset
    //    - SpeedUp.asset
    // 3. 각각 대응하는 ScriptableObject 타입 할당
    // 4. Inspector에서 이름, 설명, 아이콘 설정
    
    /*
    생성 예시:

    DamageUp.asset:
    - powerUpName: "Damage Up"
    - description: "Damage +5"
    - icon: (Sprite)
    - effect: DamageUpPowerUp (ScriptableObject 인스턴스)

    HealthUp.asset:
    - powerUpName: "Health Up"
    - description: "Max Health +50"
    - icon: (Sprite)
    - effect: HealthUpPowerUp (ScriptableObject 인스턴스)

    FireRateUp.asset:
    - powerUpName: "Fire Rate Up"
    - description: "Attack Speed x1.5"
    - icon: (Sprite)
    - effect: FireRateUpPowerUp (ScriptableObject 인스턴스)

    SpeedUp.asset:
    - powerUpName: "Speed Up"
    - description: "Movement Speed +1.5"
    - icon: (Sprite)
    - effect: SpeedUpPowerUp (ScriptableObject 인스턴스)
    */
}
