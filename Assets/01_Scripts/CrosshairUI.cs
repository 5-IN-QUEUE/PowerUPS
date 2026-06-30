using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair")]
    [SerializeField] private float lineLength     = 10f;
    [SerializeField] private float lineWidth      = 2f;
    [SerializeField] private float gapSize        = 5f;
    [SerializeField] private Color crosshairColor = Color.white;

    [Header("Hit Marker")]
    [SerializeField] private float hitLineLength = 8f;
    [SerializeField] private Color hitColor      = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] private float hitDuration   = 0.25f;

    private Image[] _hitLines = new Image[2];
    private float   _hitTimer;

    private void Awake()
    {
        BuildCrosshair();
        BuildHitmarker();
    }

    private void OnEnable()  => PlayerShoot.OnHitConfirmed += TriggerHitmarker;
    private void OnDisable() => PlayerShoot.OnHitConfirmed -= TriggerHitmarker;

    private void Update()
    {
        if (_hitTimer <= 0f) return;
        _hitTimer -= Time.deltaTime;
        SetHitAlpha(Mathf.Clamp01(_hitTimer / hitDuration));
    }

    private void TriggerHitmarker()
    {
        _hitTimer = hitDuration;
        SetHitAlpha(1f);
    }

    private void BuildCrosshair()
    {
        float offset = gapSize + lineLength * 0.5f;
        MakeLine("CH_Up",    new Vector2( 0,      offset), new Vector2(lineWidth, lineLength), crosshairColor);
        MakeLine("CH_Down",  new Vector2( 0,     -offset), new Vector2(lineWidth, lineLength), crosshairColor);
        MakeLine("CH_Left",  new Vector2(-offset,      0), new Vector2(lineLength, lineWidth), crosshairColor);
        MakeLine("CH_Right", new Vector2( offset,      0), new Vector2(lineLength, lineWidth), crosshairColor);
    }

    private void BuildHitmarker()
    {
        float len = hitLineLength * 2f;
        _hitLines[0] = MakeLine("HM_A", Vector2.zero, new Vector2(lineWidth, len), hitColor,  45f);
        _hitLines[1] = MakeLine("HM_B", Vector2.zero, new Vector2(lineWidth, len), hitColor, -45f);
        SetHitAlpha(0f);
    }

    private Image MakeLine(string goName, Vector2 pos, Vector2 size, Color color, float angle = 0f)
    {
        var go  = new GameObject(goName, typeof(Image));
        var rt  = go.GetComponent<RectTransform>();
        var img = go.GetComponent<Image>();

        rt.SetParent(transform, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        rt.localRotation    = Quaternion.Euler(0f, 0f, angle);

        img.color = color;
        return img;
    }

    private void SetHitAlpha(float a)
    {
        foreach (var img in _hitLines)
        {
            if (img == null) continue;
            var c = img.color;
            c.a   = a;
            img.color = c;
        }
    }
}
