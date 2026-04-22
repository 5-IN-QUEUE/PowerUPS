using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    private NetworkCharacterController networkCharacterController;

    private void Awake()
    {
        networkCharacterController = GetComponent<NetworkCharacterController>();
    }
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            Vector3 moveDirection = data.direction.normalized;

            networkCharacterController.Move(5 * moveDirection * Runner.DeltaTime);

            if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON1))
            {
                networkCharacterController.Jump();
            }
        }
    }
}
