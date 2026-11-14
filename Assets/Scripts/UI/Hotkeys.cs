using UnityEngine;
using UnityEngine.InputSystem;

// Klavye kisayollari (yeni Input System):
//   Space       : topla
//   1 / 2 / 3 / 4: x1 / x10 / x100 / Max alim modu
//   Esc         : ayarlari ac/kapat, acik panel varsa kapat
public class Hotkeys : MonoBehaviour
{
    SettingsPanel settings;
    TimeMachinePanel tree;

    void Start()
    {
        settings = GetComponent<SettingsPanel>();
        tree = GetComponent<TimeMachinePanel>();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (settings != null && settings.IsOpen) settings.Close();
            else if (tree != null && tree.IsOpen) tree.Close();
            else if (settings != null) settings.Open();
            return;
        }

        bool panelOpen = (settings != null && settings.IsOpen) || (tree != null && tree.IsOpen);
        if (panelOpen) return;

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            gm.Gather();
        }

        if (keyboard.digit1Key.wasPressedThisFrame) gm.SetBuyMode(BuyMode.One);
        else if (keyboard.digit2Key.wasPressedThisFrame) gm.SetBuyMode(BuyMode.Ten);
        else if (keyboard.digit3Key.wasPressedThisFrame) gm.SetBuyMode(BuyMode.Hundred);
        else if (keyboard.digit4Key.wasPressedThisFrame) gm.SetBuyMode(BuyMode.Max);
    }
}
