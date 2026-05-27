using UnityEngine;
using Fusion;
public class CameraControlScript : MonoBehaviour
{
    public float rotateX = 0;
    public GameObject WaitingUI;
    private Vector3 Offsets = new Vector3(0,1.5f,-4f);
    void Start()
{
        WaitingUI.SetActive(true);
    }
    void Update(){
        if(PlayerController.localPlayer != null){
            WaitingUI.SetActive(false);
            
            rotateX += Input.mousePositionDelta.x;
            PlayerController.localPlayer.transform.rotation = Quaternion.Euler(0,rotateX,0);

            transform.position = Vector3.Lerp(transform.position,PlayerController.localPlayer.transform.position + PlayerController.localPlayer.transform.up * Offsets.y + PlayerController.localPlayer.transform.forward * Offsets.z,Time.deltaTime * 10f);
            transform.rotation = PlayerController.localPlayer.transform.rotation;
        }
        
    }
}
