using Fusion;
using UnityEngine;

public class PlayerShoot : NetworkBehaviour
{
    [SerializeField] private GunData       stats;
    public GunData Stats => stats;
    [SerializeField] private NetworkObject bulletPrefab;
    [SerializeField] private Transform     muzzlePoint;

    public static event System.Action OnHitConfirmed;
    public static event System.Action OnReloadStart;

    [Networked] public int       Ammo        { get; set; }
    [Networked] public bool      IsReloading { get; private set; }
    [Networked] private TickTimer ReloadTimer { get; set; }
    [Networked] private TickTimer FireTimer   { get; set; }
    
    // PowerUp 시스템 추가
    [Networked] private float DamageMultiplier      { get; set; }
    [Networked] private float FireRateMultiplier   { get; set; }
    [Networked] private int   PelletBonus          { get; set; }
    [Networked] private int   BounceBonus          { get; set; }
    [Networked] private float AccuracyBonus        { get; set; } // 양수 = 퍼짐 증가
    [Networked] private float BulletSpeedMultiplier { get; set; }

    private ChangeDetector _changes;

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        if (HasStateAuthority)
        {
            Ammo                  = GunData.MaxAmmo;
            DamageMultiplier      = 1f;
            FireRateMultiplier    = 1f;
            PelletBonus           = 0;
            BounceBonus           = 0;
            AccuracyBonus         = 0f;
            BulletSpeedMultiplier = 1f;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority && IsReloading && ReloadTimer.Expired(Runner))
        {
            IsReloading = false;
            Ammo        = GunData.MaxAmmo;
        }

        if (!GetInput(out NetworkInputData data)) return;

        if (data.buttons.IsSet(NetworkInputData.RELOAD) && !IsReloading && Ammo < GunData.MaxAmmo)
        {
            if (HasStateAuthority)
            {
                IsReloading = true;
                ReloadTimer = TickTimer.CreateFromSeconds(Runner, 1f / stats.reloadSpeed);
            }
        }

        bool canFire = !IsReloading && Ammo > 0 && FireTimer.ExpiredOrNotRunning(Runner);
        if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0) && canFire)
        {
            if (HasStateAuthority)
            {
                FireTimer = TickTimer.CreateFromSeconds(Runner, 1f / (stats.fireRate * FireRateMultiplier));
                Vector3 aimDir = Quaternion.Euler(data.rotationX, data.rotationY, 0) * Vector3.forward;
                for (int i = 0; i < stats.pellet + PelletBonus; i++)
                    SpawnBullet(aimDir);
                Ammo = Mathf.Max(0, Ammo - 1);
            }
        }
    }

    public override void Render()
    {
        foreach (var change in _changes.DetectChanges(this))
        {
            if (change == nameof(IsReloading) && IsReloading)
                OnReloadStart?.Invoke();
        }
    }

    // 라운드 재시작 시 서버에서 호출
    public void ResetGun()
    {
        if (!HasStateAuthority) return;
        Ammo                  = GunData.MaxAmmo;
        IsReloading           = false;
        ReloadTimer           = default;
        FireTimer             = default;
        DamageMultiplier      = 1f;
        FireRateMultiplier    = 1f;
        PelletBonus           = 0;
        BounceBonus           = 0;
        AccuracyBonus         = 0f;
        BulletSpeedMultiplier = 1f;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void Rpc_HitConfirmed()
    {
        OnHitConfirmed?.Invoke();
    }

    private void SpawnBullet(Vector3 baseDir)
    {
        float spread = Mathf.Max(0f, stats.accuracy + AccuracyBonus);
        Vector3 spreadVec = new Vector3(
            UnityEngine.Random.Range(-spread, spread),
            UnityEngine.Random.Range(-spread, spread),
            UnityEngine.Random.Range(-spread, spread)
        );
        Vector3 dir = (baseDir + spreadVec).normalized;

        Vector3 spawnPos = muzzlePoint != null
            ? muzzlePoint.position
            : transform.position + transform.forward * 0.6f + Vector3.up * 1.4f;

        float finalDamage = stats.damage * DamageMultiplier;
        float finalSpeed  = stats.bulletSpeed * BulletSpeedMultiplier;

        Runner.Spawn(
            bulletPrefab,
            spawnPos,
            Quaternion.LookRotation(dir),
            Object.InputAuthority,
            (runner, obj) => obj.GetComponent<BulletScript>().Init(
                finalDamage,
                finalSpeed,
                stats.bounceCount + BounceBonus,
                Object.InputAuthority,
                dir
            )
        );
    }

    // ==================== PowerUp 효과 메서드 ====================

    public void ApplyDamageMultiplier(int flatIncrease)
    {
        if (!HasStateAuthority) return;
        DamageMultiplier += (flatIncrease / (float)stats.damage);
        Debug.Log($"[PlayerShoot] Damage multiplier: {DamageMultiplier}");
    }

    public void ApplyFireRateMultiplier(float multiplier)
    {
        if (!HasStateAuthority) return;
        FireRateMultiplier *= multiplier;
        Debug.Log($"[PlayerShoot] Fire rate multiplier: {FireRateMultiplier}");
    }

    public void IncreasePellets(int amount)
    {
        if (!HasStateAuthority) return;
        PelletBonus += amount;
        Debug.Log($"[PlayerShoot] Pellets: {stats.pellet + PelletBonus}");
    }

    public void IncreaseRicochets(int amount)
    {
        if (!HasStateAuthority) return;
        BounceBonus += amount;
        Debug.Log($"[PlayerShoot] Bounces: {stats.bounceCount + BounceBonus}");
    }

    public void ChangeAccuracy(float delta)
    {
        if (!HasStateAuthority) return;
        AccuracyBonus += delta;
        Debug.Log($"[PlayerShoot] Accuracy: {stats.accuracy + AccuracyBonus}");
    }

    public void ApplyBulletSpeedMultiplier(float multiplier)
    {
        if (!HasStateAuthority) return;
        BulletSpeedMultiplier *= multiplier;
        Debug.Log($"[PlayerShoot] BulletSpeed multiplier: {BulletSpeedMultiplier}");
    }
}
