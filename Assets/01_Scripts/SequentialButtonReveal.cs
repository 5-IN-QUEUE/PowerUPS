using System.Collections;
using UnityEngine;

// 시작하면 지정한 버튼(또는 UI 오브젝트)들이 순서대로 하나씩 나타난다.
// 각 버튼은 기존에 있던 위치에서 살짝 확대되며 등장한다.
[RequireComponent(typeof(RectTransform))]
public class SequentialButtonReveal : MonoBehaviour
{
    [Header("나타낼 순서대로 버튼(UI)들을 넣기")]
    [SerializeField] private RectTransform[] targets;

    [Header("타이밍")]
    [SerializeField] private float startDelay = 1f;   // 게임 시작 후 첫 버튼까지 대기
    [SerializeField] private float duration = 0.5f;     // 각 버튼 애니메이션 시간

    [Header("크기")]
    [SerializeField] private float startScale = 3f;    // 등장 시작 크기 배율

    private void OnEnable()
    {
        StartCoroutine(RevealRoutine());
    }

    private IEnumerator RevealRoutine()
    {
        // 시작 전엔 전부 숨김
        foreach (var t in targets)
        {
            if (t == null) continue;
            t.gameObject.SetActive(false);
        }

        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        foreach (var t in targets)
        {
            if (t == null) continue;
            yield return StartCoroutine(RevealOne(t));
        }
    }

    private IEnumerator RevealOne(RectTransform t)
    {
        t.gameObject.SetActive(true);

        Vector3 baseScale = t.localScale;
        Vector3 fromScale = baseScale * startScale;

        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(time / duration);
            float eased = 1f - (1f - p) * (1f - p); // ease-out

            t.localScale = Vector3.LerpUnclamped(fromScale, baseScale, eased);
            yield return null;
        }

        t.localScale = baseScale;
    }
}
