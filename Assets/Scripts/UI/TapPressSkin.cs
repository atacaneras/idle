using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Ana butonun (CEZALANDIR / IYILIK YAP) ic yuzeyi: basili tutarken Tap_Button_UI_In_Tappin sprite'ina gecer.
// HUDController tarafindan otomatik eklenir; sahnede elle bir sey gerekmez.
public class TapPressSkin : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    Image target;
    Sprite normal, pressed;

    public void Init(Image img, Sprite normalSprite, Sprite pressedSprite)
    {
        target = img; normal = normalSprite; pressed = pressedSprite;
    }

    // Olaylar butonun kendisine gelir; bu bilesen ic yuzeyin uzerinde oldugu icin ust nesneden dinler.
    void Start()
    {
        var parent = transform.parent;
        if (parent != null && parent.GetComponent<TapPressRelay>() == null)
            parent.gameObject.AddComponent<TapPressRelay>().skin = this;
    }

    public void OnPointerDown(PointerEventData e) { Set(true); }
    public void OnPointerUp(PointerEventData e) { Set(false); }
    public void OnPointerExit(PointerEventData e) { Set(false); }

    public void Set(bool down)
    {
        if (target == null) return;
        target.sprite = down && pressed != null ? pressed : normal;
    }
}

// Buton nesnesine eklenir, basma olaylarini ic yuzey bilesenine iletir.
public class TapPressRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public TapPressSkin skin;
    public void OnPointerDown(PointerEventData e) { if (skin != null) skin.Set(true); }
    public void OnPointerUp(PointerEventData e) { if (skin != null) skin.Set(false); }
    public void OnPointerExit(PointerEventData e) { if (skin != null) skin.Set(false); }
}
