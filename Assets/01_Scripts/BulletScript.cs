using Fusion;
using UnityEngine;

public class BulletScript : NetworkBehaviour
{
    [Networked] private float     Damage      { get; set; }
    [Networked] private float     Speed       { get; set; }
    [Networked] private int       BouncesLeft { get; set; }
    [Networked] private PlayerRef Shooter     { get; set; }
    [Networked] private Vector3   MoveDir     { get; set; }
    [Networked] private TickTimer LifeTimer   { get; set; }
    [Networked] private TickTimer GraceTimer  { get; set; } // 스폰 직후 자신 충돌 방지

    public void Init(float damage, float speed, int bounces, PlayerRef shooter, Vector3 dir)
    {
        Damage      = damage;
        Speed       = speed;
        BouncesLeft = bounces;
        Shooter     = shooter;
        MoveDir     = dir.normalized;
        LifeTimer   = TickTimer.CreateFromSeconds(Runner, 10f);
        GraceTimer  = TickTimer.CreateFromSeconds(Runner, 0.12f);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (LifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        float   dist   = Speed * Runner.DeltaTime;
        Vector3 origin = transform.position;

        bool graceOver = GraceTimer.Expired(Runner);

        if (graceOver && Physics.Raycast(origin, MoveDir, out RaycastHit hit, dist + 0.05f))
        {
            var player = hit.collider.GetComponentInParent<PlayerController>();

            if (player != null && player.Object.InputAuthority != Shooter)
            {
                // 적 명중
                player.TakeDamage((int)Damage);

                // 히트마커 RPC → 발사한 플레이어 클라이언트로 전송
                if (Runner.TryGetPlayerObject(Shooter, out var shooterObj))
                {
                    var ps = shooterObj.GetComponent<PlayerShoot>();
                    if (ps != null) ps.Rpc_HitConfirmed();
                }

                Runner.Despawn(Object);
                return;
            }

            if (player == null)
            {
                // 환경(벽 등) 충돌
                if (BouncesLeft > 0)
                {
                    transform.position = hit.point + hit.normal * 0.02f;
                    MoveDir = Vector3.Reflect(MoveDir, hit.normal);
                    BouncesLeft--;
                    return;
                }
                else
                {
                    Runner.Despawn(Object);
                    return;
                }
            }
            // 자신 충돌 → 통과
        }

        transform.position += MoveDir * dist;
    }
}
