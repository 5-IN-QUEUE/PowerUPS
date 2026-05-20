using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    private NetworkCharacterController networkCharacterController;

    [Networked]public Color playerColor{get; set;}
    [Networked]public string playerName{get; set;}
    private void Awake()
    {
        networkCharacterController = GetComponent<NetworkCharacterController>();
    }
    private void Start()
    {
        playerColor = new Color(Random.Range(0f,1f),Random.Range(0f,1f),Random.Range(0f,1f));
        playerName = $"Player{Random.Range(0,999999)}";
        transform.GetChild(0).GetComponent<TextMesh>().text = playerName;
        transform.GetComponent<MeshRenderer>().material.color = playerColor;
    }
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data)) //get data in server
        {
            Vector3 moveDirection = data.direction.normalized;
            networkCharacterController.Move(5 * moveDirection * Runner.DeltaTime);
            if (data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0)){
                networkCharacterController.Jump();
            }
        }
    }
}
