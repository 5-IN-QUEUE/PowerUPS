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
        // 커서 잠금은 로컬 플레이어 초기화 후 Update()에서 처리한다.
        // Awake에서 잠그면 로비로 돌아갈 때 커서가 복구되지 않는 문제가 생긴다.
    }

    private void OnDestroy()
    {
        // Play 씬이 언로드될 때(→ 로비) 커서를 반드시 복구한다.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void Update()
    {
        // 로컬 플레이어 생성 전까지 대기
        if (!_initialized)
        {
            if (PlayerController.localPlayer == null) return;
            InitCamera();
        }

        // 카드 선택 중이거나(내 화면에 카드가 떠 있거나) 증강 선택 페이즈(카드 UI가
        // 안 뜨는 승자 쪽 포함) 동안은 커서를 보여준다. UpgradeCardSelect.IsSelecting만
        // 보면 카드 패널이 없는 쪽은 입력은 막혔는데 커서는 계속 잠겨 있어
        // 아무것도 못 하는 것처럼 보이는 문제가 있었다.
        bool isAugmentSelectPhase = GameFlowManager.Instance != null
            && GameFlowManager.Instance.CurrentState == GameFlowManager.GameState.AugmentSelect;
        bool showCursor = UpgradeCardSelect.IsSelecting || isAugmentSelectPhase;
        Cursor.visible   = showCursor;
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;

        HandleMouseInput();
        ApplyPitchToCamera();
    }

    private void InitCamera()
    {
        _cameraPivot = PlayerController.localPlayer.transform.GetChild(1);

        // 부모 설정은 딱 한 번만
        waitingUI.SetActive(false);
        transform.SetParent(_cameraPivot);
        transform.parent.parent.GetChild(2).gameObject.SetActive(false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        waitingUI.SetActive(false);
        _initialized = true;
    }

    private void HandleMouseInput()
    {
        if (UpgradeCardSelect.IsSelecting) return;
        if (GameFlowManager.Instance != null &&
            GameFlowManager.Instance.CurrentState == GameFlowManager.GameState.AugmentSelect) return;

        Yaw   += Input.GetAxisRaw("Mouse X") * sensitivity;
        Pitch -= Input.GetAxisRaw("Mouse Y") * sensitivity;
        Pitch  = Mathf.Clamp(Pitch, -pitchLimit, pitchLimit);
    }

    private void ApplyPitchToCamera()
    {
        // 상하 회전(Pitch)만 카메라 피벗에 적용
        // Yaw(좌우)는 플레이어 몸체가 FixedUpdateNetwork에서 처리
        _cameraPivot.localRotation = Quaternion.Euler(Pitch, 0f, 0f);
    }
}