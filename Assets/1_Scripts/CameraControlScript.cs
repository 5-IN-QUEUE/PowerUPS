using UnityEngine;
using Fusion;
public class CameraControlScript : MonoBehaviour
{
    private float rotateX = 0;
    private float rotateY = 0;
    public GameObject WaitingUI;
    private Vector3 Offsets = new Vector3(0, 1f, -4f);

    void Start()
    {
        WaitingUI.SetActive(true);
    }

    void Update()
    {
        if (PlayerController.localPlayer != null)
        {
            WaitingUI.SetActive(false);

            rotateX += Input.mousePositionDelta.x;
            rotateY += Input.mousePositionDelta.y;
            if(rotateY > 80f)
                rotateY = 80f;
            else if(rotateY < -80f)
                rotateY = -80f;

            // ✅ NetworkInputHandler에 회전값 전달
            var inputHandler = PlayerController.localPlayer
                .GetComponent<NetworkInputHandler>(); // 또는 찾는 방법
            // rotationY 공유를 위해 static 사용
            NetworkInputHandler.rotationY = rotateX;

            // ✅ 카메라 위치/회전은 카메라가 직접 계산
            Vector3 targetPos = PlayerController.localPlayer.transform.position
                + Vector3.up * Offsets.y
                + PlayerController.localPlayer.transform.forward * Offsets.z;

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Euler(0, rotateX, 0);
            PlayerController.localPlayer.transform.GetChild(2).localRotation = Quaternion.Euler(-rotateY, 0, 0);

        }
    }
}
