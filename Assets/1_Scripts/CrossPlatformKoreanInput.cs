using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
 
[RequireComponent(typeof(InputField))]
public class CrossPlatformKoreanInput : MonoBehaviour, IPointerClickHandler
{
    [Header("연결 설정")]
    [SerializeField] private InputField _inputField;
 
    [Header("이벤트")]
    public UnityEngine.Events.UnityEvent<string> OnValueChanged;
    public UnityEngine.Events.UnityEvent<string> OnConfirmed;
    public UnityEngine.Events.UnityEvent         OnCancelled;
 
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ShowKoreanInput(
        string defaultValue,
        string gameObjectName,
        string callbackMethod);
 
    [DllImport("__Internal")]
    private static extern void HideKoreanInput();
#endif
 
    private void Awake()
    {
        if (_inputField == null)
            _inputField = GetComponent<InputField>();
 
#if UNITY_WEBGL && !UNITY_EDITOR
        _inputField.interactable = true;
        // ★ readOnly 제거 — Unity InputField가 직접 받는 입력은 없으므로 문제 없음
        //   JS input이 모든 입력을 받아서 text를 덮어쓰는 방식으로 동작
        _inputField.readOnly = false;
#else
        _inputField.onValueChanged.AddListener(OnNativeValueChanged);
        _inputField.onEndEdit.AddListener(OnNativeEndEdit);
#endif
    }
 
    public void OnPointerClick(PointerEventData eventData)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ShowKoreanInput(
            _inputField.text,
            gameObject.name,
            nameof(OnJSValueChanged)
        );
#endif
    }
 
    // ── WebGL 콜백 ─────────────────────────────
 
    /// <summary>조합 중 실시간 호출 — "확정텍스트|조합중글자" 형식</summary>
    public void OnComposing(string value)
    {
        var parts = value.Split('|');
        if (parts.Length != 2) return;
        _inputField.text = parts[0] + parts[1];
        OnValueChanged?.Invoke(_inputField.text);
    }
 
    /// <summary>조합 완료 또는 영문/숫자 입력 확정 시</summary>
    public void OnJSValueChanged(string value)
    {
        _inputField.text = value;
        OnValueChanged?.Invoke(value);
    }
 
    /// <summary>Enter 확정</summary>
    public void OnInputConfirmed(string value)
    {
        _inputField.text = value;
        OnValueChanged?.Invoke(value);
        OnConfirmed?.Invoke(value);
    }
 
    /// <summary>ESC 취소</summary>
    public void OnInputCancelled(string _)
    {
        OnCancelled?.Invoke();
    }
 
    // ── Windows / macOS ─────────────────────────
 
    private void OnNativeValueChanged(string value)
    {
        OnValueChanged?.Invoke(value);
    }
 
    private void OnNativeEndEdit(string value)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            OnConfirmed?.Invoke(value);
    }
 
    // ── 유틸 ────────────────────────────────────
 
    public void SetText(string value) => _inputField.text = value;
    public string GetText()           => _inputField.text;
 
    private void OnDisable()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        HideKoreanInput();
#endif
    }
 
    private void OnDestroy()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        HideKoreanInput();
#else
        if (_inputField != null)
        {
            _inputField.onValueChanged.RemoveListener(OnNativeValueChanged);
            _inputField.onEndEdit.RemoveListener(OnNativeEndEdit);
        }
#endif
    }
}