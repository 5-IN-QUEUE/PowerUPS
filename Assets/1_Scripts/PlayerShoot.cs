using Fusion;
using Fusion.Addons.Physics;
using Unity.Mathematics;
using UnityEngine;

public class PlayerShoot : NetworkBehaviour
{
    [SerializeField] NetworkRigidbody3D nrb;
    Rigidbody rb;
    PlayerInputActions inputs;
    public Animator anim;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        nrb = GetComponent<NetworkRigidbody3D>();
        inputs = new PlayerInputActions();
    }
    void OnEnable(){inputs.Enable();}
    void OnDisable(){inputs.Disable();}
    Vector2 AnimMove = Vector2.zero;
    void Update()
    {
        Vector2 tv = inputs.Player.Move.ReadValue<Vector2>();
        AnimMove = Vector2.Lerp(AnimMove,tv,Time.deltaTime * 3f);
        float speed = math.abs(AnimMove.x) + math.abs(AnimMove.y);
        if(speed < 0.05f){speed = 0f;}
        anim.SetFloat("speed",speed);
        anim.SetFloat("MoveX",AnimMove.x);
        anim.SetFloat("MoveY",AnimMove.y);
        anim.SetBool("IsShooting",Input.GetMouseButton(0));
    }
}
