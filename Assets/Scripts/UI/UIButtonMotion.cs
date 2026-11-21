using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Shared, subtle motion for mouse, keyboard and gamepad button feedback.
public class UIButtonMotion : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler
{
    [SerializeField] float hoverScale = 1.035f;
    [SerializeField] float pressedScale = 0.965f;
    [SerializeField] float settleSpeed = 20f;

    RectTransform rect;
    Button button;
    Vector3 baseScale;
    bool hovered, pressed, selected;
    float punchTime = -1f, punchDuration = 0.18f, punchAmount = 0.1f;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        button = GetComponent<Button>();
        if (rect != null) baseScale = rect.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData) { if (CanAnimate()) hovered = true; }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; }
    public void OnPointerDown(PointerEventData eventData) { if (CanAnimate()) pressed = true; }
    public void OnPointerUp(PointerEventData eventData) { pressed = false; }
    public void OnSelect(BaseEventData eventData) { if (CanAnimate()) selected = true; }
    public void OnDeselect(BaseEventData eventData) { selected = false; }

    public void Punch(float amount = 0.1f, float duration = 0.18f)
    {
        if (!CanAnimate()) return;
        punchAmount = amount;
        punchDuration = Mathf.Max(0.05f, duration);
        punchTime = 0f;
    }

    void Update()
    {
        if (rect == null) return;
        bool active = button == null || button.IsInteractable();
        float stateScale = !active ? 1f : pressed ? pressedScale : (hovered || selected ? hoverScale : 1f);
        float punch = 1f;
        if (punchTime >= 0f)
        {
            punchTime += Time.unscaledDeltaTime / punchDuration;
            if (punchTime >= 1f) punchTime = -1f;
            else punch = 1f + punchAmount * Mathf.Sin(punchTime * Mathf.PI);
        }

        Vector3 target = baseScale * (stateScale * punch);
        float blend = 1f - Mathf.Exp(-settleSpeed * Time.unscaledDeltaTime);
        rect.localScale = Vector3.Lerp(rect.localScale, target, blend);
    }

    bool CanAnimate() { return button == null || button.IsInteractable(); }

    void OnDisable()
    {
        hovered = pressed = selected = false;
        punchTime = -1f;
        if (rect != null) rect.localScale = baseScale;
    }
}
