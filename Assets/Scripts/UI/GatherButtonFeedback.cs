using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Toplama butonuna HUDController tarafindan otomatik eklenir.
// Tiklanan noktada yuzen "+X" cikarir ve butona kucuk bir "punch" animasyonu verir.
// Konum EventSystem'den gelir; bu yuzden yeni Input System ile de sorunsuz calisir.
public class GatherButtonFeedback : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    [SerializeField] float punchDuration = 0.18f;
    [SerializeField] float punchAmount = 0.12f;

    FloatingTextSpawner spawner;
    RectTransform rt;
    Button button;
    UIButtonMotion motion;

    public void Init(FloatingTextSpawner s)
    {
        spawner = s;
        rt = (RectTransform)transform;
        button = GetComponent<Button>();
        motion = GetComponent<UIButtonMotion>();
        if (motion == null) motion = gameObject.AddComponent<UIButtonMotion>();
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        Feedback();
        if (spawner != null && CanGather()) spawner.Spawn(Label(), e.position);
    }

    // Klavye / gamepad ile "Submit" yapilirsa butonun ortasinda cikar
    public void OnSubmit(BaseEventData e)
    {
        Feedback();
        if (spawner != null && CanGather()) spawner.SpawnAt(Label(), rt);
    }

    bool CanGather()
    {
        return GameManager.Instance != null && GameManager.Instance.CurrentAge != null
            && (button == null || button.IsInteractable());
    }

    string Label() { return "+" + NumberFormatter.Format(GameManager.Instance.ClickValue); }

    void Feedback() { if (motion != null && CanGather()) motion.Punch(punchAmount, punchDuration); }

}
