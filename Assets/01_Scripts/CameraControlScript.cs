using UnityEngine;

public class CameraController : MonoBehaviour
{
    // InputHandler에서 참조할 수 있도록 싱글톤
    public static CameraController Instance { get; private set; }

    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float pitchLimit  = 80f;
    [SerializeField] private GameObject waitingUI;

    // 외부(InputHandler)에서 읽어가는 회전값
    public float Yaw   { get; private set; } = 0f;  // 좌우 (플레이어 몸 회전)
    public float Pitch { get; private set; } = 0f;  // 상하 (카메라만)

    private bool _initialized = false;
    private Transform _cameraPivot; // 플레이어의 child[1] (머리/눈 위치)

    private void Awake()
    {
        waitingUI.SetActive(true);
        Instance = this;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void Update()
    {
        // 로컬 플레이어 생성 전까지 대기
        if (!_initialized)
        {
            if (PlayerController.localPlayer == null) return;
            InitCamera();
        }

        HandleMouseInput();
        ApplyPitchToCamera();
    }

    private void InitCamera()
    {
        _cameraPivot = PlayerController.localPlayer.transform.GetChild(1);

        // 부모 설정은 딱 한 번만
        waitingUI.SetActive(false);
        transform.SetParent(_cameraPivot);
        transform.GetChild(2).gameObject.SetActive(false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        waitingUI.SetActive(false);
        _initialized = true;
    }

    private void HandleMouseInput()
    {
        // GetAxisRaw 사용 → Project Settings > Input에서 감도 조절 가능
        Yaw   += Input.GetAxisRaw("Mouse X") * sensitivity;
        Pitch -= Input.GetAxisRaw("Mouse Y") * sensitivity; // -: 마우스 위 = 위를 봄
        Pitch  = Mathf.Clamp(Pitch, -pitchLimit, pitchLimit);
    }

    private void ApplyPitchToCamera()
    {
        // 상하 회전(Pitch)만 카메라 피벗에 적용
        // Yaw(좌우)는 플레이어 몸체가 FixedUpdateNetwork에서 처리
        _cameraPivot.localRotation = Quaternion.Euler(Pitch, 0f, 0f);
    }
}