using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

// 해상도 / 화면 모드 / 언어 / 설정 창 담당 (GameManager에서 분리)
public class SettingsManager : MonoBehaviour
{
	public static SettingsManager instance;

	[Header("Input")]
	public InputActionAsset inputActions;
	private InputAction settingsAction;

	[Header("설정 창")]
	public Button     settingButton;
	public Button     closeButton;
	public GameObject settingUI;

	void Awake()
	{
		if (instance == null) instance = this;
		else { Destroy(gameObject); }
	}

	void Start()
	{
		// 버튼은 전부 코드로 연결 (인스펙터 onClick 설정 불필요)
		if (settingButton != null) settingButton.onClick.AddListener(OpenSettingUI);
		if (closeButton   != null) closeButton.onClick.AddListener(CloseSettingUI);

		// ESC 키로 설정창 토글 (InputAction)
		if (inputActions != null)
		{
			InputActionMap gameplay = inputActions.FindActionMap("Player");
			settingsAction = gameplay?.FindAction("Settings");
			if (settingsAction != null)
			{
				settingsAction.performed += OnSettingsPerformed;
				settingsAction.Enable();
			}
		}
	}

	void OnDestroy()
	{
		if (settingsAction != null)
		{
			settingsAction.performed -= OnSettingsPerformed;
			settingsAction.Disable();
		}
	}

	private void OnSettingsPerformed(InputAction.CallbackContext context) => ToggleSettingUI();

	// ESC: 닫혀있으면 열고, 열려있으면 닫기
	public void ToggleSettingUI()
	{
		if (settingUI == null) return;
		if (settingUI.activeSelf) CloseSettingUI();
		else                      OpenSettingUI();
	}
	
	public void OpenSettingUI()
	{
		settingUI.SetActive(true);
		SoundManager.instance?.PlaySFX("UIClick");
	}

	public void CloseSettingUI()
	{
		settingUI.SetActive(false);
		SoundManager.instance?.PlaySFX("UIClick");
	}
}
