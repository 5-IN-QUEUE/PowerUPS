using Unity.Mathematics;
using UnityEngine;

public class ClientAnimationScript : MonoBehaviour
{
    public static ClientAnimationScript Instance { get; set; }

    public Animator localAnimator;

    private PlayerInputActions _inputs;
    private Vector2            _animMove;
    private PlayerShoot        _localShoot;

    private void Awake()
    {
        Instance = this;
        _inputs  = new PlayerInputActions();
    }

    private void OnEnable()
    {
        _inputs.Enable();
        PlayerShoot.OnReloadStart += HandleReload;
    }

    private void OnDisable()
    {
        _inputs.Disable();
        PlayerShoot.OnReloadStart -= HandleReload;
    }

    private void Update()
    {
        // 로컬 플레이어 스폰 후 PlayerShoot 참조 획득
        if (_localShoot == null && PlayerController.localPlayer != null)
            _localShoot = PlayerController.localPlayer.GetComponent<PlayerShoot>();

        if (_localShoot == null || localAnimator == null) return;

        // 이동 애니메이션
        // Vector2 tv = _inputs.Player.Move.ReadValue<Vector2>();
        // _animMove = Vector2.Lerp(_animMove, tv, Time.deltaTime * 3f);
        // float speed = math.abs(_animMove.x) + math.abs(_animMove.y);
        // if (speed < 0.05f) speed = 0f;
        // localAnimator.SetFloat("speed", speed);
        // localAnimator.SetFloat("MoveX", _animMove.x);
        // localAnimator.SetFloat("MoveY", _animMove.y);

        // 사격 애니메이션 (누르는 중 true, 떼면 false)
        bool isShooting = Input.GetMouseButton(0) && !_localShoot.IsReloading && _localShoot.Ammo > 0;
        localAnimator.SetBool("IsShooting", isShooting);
    }

    private void HandleReload()
    {
        localAnimator.SetTrigger("Reload");
    }
}
