using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    private NetworkCharacterController networkCharacterController;
    public static GameObject localPlayer;
    [Networked]public Color playerColor{get; set;}
    [Networked]public string playerName{get; set;}
    
    private void Awake()
    {
        networkCharacterController = GetComponent<NetworkCharacterController>();
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SendChatMessage(string senderName, string message)
    {
        // 채팅 UI 찾아서 메시지 표시
        FindObjectOfType<ChattingScript>().AddChatMessage(senderName, message);
    }
    public override void Spawned(){// Fusion의 네트워크 생명주기 사용
        if (HasStateAuthority){
            playerColor = new Color(Random.Range(0f,1f),Random.Range(0f,1f),Random.Range(0f,1f));
            playerName = $"Player{Random.Range(0,999999)}";
        }
        if(HasInputAuthority){
            localPlayer = gameObject;
        }
    }
    private void Start()
    {
        transform.GetChild(0).GetComponent<TextMesh>().text = playerName;
        gameObject.name = playerName;
        transform.GetComponent<MeshRenderer>().material.color = playerColor;
    }
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data)) //get data in server
        {
            Vector3 moveDirection = data.direction.normalized;
            networkCharacterController.Move(5 * moveDirection * Runner.DeltaTime);
            if (data.Jump){
                networkCharacterController.Jump();
                data.Jump = false;
            }
        }
    }
}
