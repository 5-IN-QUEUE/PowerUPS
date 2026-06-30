using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/GunData", fileName = "NewGunData")]
public class GunData : ScriptableObject
{
    public const int MaxAmmo = 30;

    public float damage      = 10f;
    public float bulletSpeed = 30f;
    public float fireRate    = 8f;   // 초당 발사 횟수
    public int   bounceCount = 0;
    public float reloadSpeed = 1f;   // 배율. 2f = 2배 빠른 재장전
    public int   pellet      = 1;
    public float accuracy    = 0.01f; // 퍼짐 반경 (작을수록 정확)
    public AudioClip FireSound;
    public Sprite GunIMG;
}
