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

    // 실제 이동은 StateAuthority(호스트)에서만 계산되므로, transform.position을
    // 직접 갱신하는 것만으로는 다른 클라이언트에 동기화되지 않는다(NetworkTransform 미사용).
    // 이 값을 통해 위치를 명시적으로 복제하고, Render()에서 모든 클라이언트가 반영한다.
    [Networked] private Vector3 Position { get; set; }

    public void Init(float damage, float speed, int bounces, PlayerRef shooter, Vector3 dir)
    {
        Damage      = damage;
        Speed       = speed;
        BouncesLeft = bounces;
        Shooter     = shooter;
        MoveDir     = dir.normalized;
        Position    = transform.position;
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
                player.TakeDamage((int)Damage, Shooter);

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
                    Position = transform.position;
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
        Position = transform.position;
    }

    public override void Render()
    {
        // 호스트는 FixedUpdateNetwork에서 이미 transform.position을 직접 갱신했으므로 제외
        if (HasStateAuthority) return;
        transform.position = Position;
    }
}
