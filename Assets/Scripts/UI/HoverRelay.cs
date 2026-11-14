using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Herhangi bir UI nesnesine fare giris/cikis/tik olayi baglamak icin.
public class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Action onEnter, onExit, onClick;
    public void OnPointerEnter(PointerEventData e) { if (onEnter != null) onEnter(); }
    public void OnPointerExit(PointerEventData e) { if (onExit != null) onExit(); }
    public void OnPointerClick(PointerEventData e) { if (onClick != null) onClick(); }
}
