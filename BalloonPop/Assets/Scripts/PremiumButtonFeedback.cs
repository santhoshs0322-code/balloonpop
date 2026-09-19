using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PremiumButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private bool pressed;
    private Graphic label;
    void Awake() { label = transform.Find("Lbl")?.GetComponent<Graphic>(); }
    public void OnPointerDown(PointerEventData e) { pressed = GetComponent<Button>().IsInteractable(); }
    public void OnPointerUp(PointerEventData e) { pressed = false; }
    public void OnPointerExit(PointerEventData e) { pressed = false; }
    void OnDisable() { pressed = false; if (label) label.transform.localScale = Vector3.one; }
    void LateUpdate()
    {
        if (label) label.transform.localScale = Vector3.Lerp(label.transform.localScale,
            Vector3.one * (pressed ? 0.94f : 1f), 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
    }
}
