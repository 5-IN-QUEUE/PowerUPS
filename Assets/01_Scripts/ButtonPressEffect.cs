using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float pressedScale = 0.9f;
    [SerializeField] private float duration = 0.08f;

    private RectTransform _rect;
    private Vector3 _originalScale;
    private Coroutine _scaleCoroutine;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _originalScale = _rect.localScale;
    }

    public void OnPointerDown(PointerEventData eventData) => AnimateTo(_originalScale * pressedScale);

    public void OnPointerUp(PointerEventData eventData) => AnimateTo(_originalScale);

    public void OnPointerExit(PointerEventData eventData) => AnimateTo(_originalScale);

    private void AnimateTo(Vector3 target)
    {
        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        _scaleCoroutine = StartCoroutine(ScaleRoutine(target));
    }

    private IEnumerator ScaleRoutine(Vector3 target)
    {
        Vector3 start = _rect.localScale;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _rect.localScale = Vector3.Lerp(start, target, t / duration);
            yield return null;
        }
        _rect.localScale = target;
    }
}
