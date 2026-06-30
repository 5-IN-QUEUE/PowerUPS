using Fusion;
using UnityEngine;

public class PlayerShoot : NetworkBehaviour
{
    [SerializeField] private GunData       stats;
    [SerializeField] private NetworkObject bulletPrefab;
    [SerializeField] private Transform     muzzlePoint;

    public static event System.Action OnHitConfirmed;
    public static event System.Action OnReloadStart;

    [Networked] public int       Ammo        { get; set; }
    [Networked] public bool      IsReloading { get; private set; }
    [Networked] private TickTimer ReloadTimer { get; set; }
    [Networked] private TickTimer FireTimer   { get; set; }

    private ChangeDetector _changes;

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        if (HasStateAuthority)
            Ammo = GunData.MaxAmmo;
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
                ReloadTimer = TickTimer.CreateFromSeconds(Runner, 2f / stats.reloadSpeed);
            }
        }

        bool canFire = !IsReloading && Ammo > 0 && FireTimer.ExpiredOrNotRunning(Runner);
        if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0) && canFire)
        {
            if (HasStateAuthority)
            {
                FireTimer = TickTimer.CreateFromSeconds(Runner, 1f / stats.fireRate);
                Vector3 aimDir = Quaternion.Euler(data.rotationX, data.rotationY, 0) * Vector3.forward;
                for (int i = 0; i < stats.pellet; i++)
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    public void Rpc_HitConfirmed()
    {
        OnHitConfirmed?.Invoke();
    }

    private void SpawnBullet(Vector3 baseDir)
    {
        Vector3 spread = new Vector3(
            UnityEngine.Random.Range(-stats.accuracy, stats.accuracy),
            UnityEngine.Random.Range(-stats.accuracy, stats.accuracy),
            UnityEngine.Random.Range(-stats.accuracy, stats.accuracy)
        );
        Vector3 dir = (baseDir + spread).normalized;

        Vector3 spawnPos = muzzlePoint != null
            ? muzzlePoint.position
            : transform.position + transform.forward * 0.6f + Vector3.up * 1.4f;

        Runner.Spawn(
            bulletPrefab,
            spawnPos,
            Quaternion.LookRotation(dir),
            Object.InputAuthority,
            (runner, obj) => obj.GetComponent<BulletScript>().Init(
                stats.damage,
                stats.bulletSpeed,
                stats.bounceCount,
                Object.InputAuthority,
                dir
            )
        );
    }
}
