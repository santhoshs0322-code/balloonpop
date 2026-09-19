using UnityEngine;
using UnityEngine.UI;

// Fits the complete design inside the safe area on phones, tablets and windowed players.
[RequireComponent(typeof(CanvasScaler))]
public class ResponsiveGameUI : MonoBehaviour
{
    Rect previous;
    Vector2 dimensions;
    void LateUpdate()
    {
        Rect safe = Screen.safeArea;
        if (safe.width <= 0 || safe.height <= 0) return;
        if (previous == safe && dimensions == new Vector2(Screen.width, Screen.height)) return;
        previous = safe;
        dimensions = new Vector2(Screen.width, Screen.height);
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        float scale = Mathf.Min(safe.width / 1080f, safe.height / 1920f);
        scaler.scaleFactor = scale;
        foreach (RectTransform child in transform)
        {
            if (child.name == "BgDimOverlay" || child.anchorMin != Vector2.zero || child.anchorMax != Vector2.one) continue;
            child.offsetMin = safe.min / scale;
            child.offsetMax = (safe.max - dimensions) / scale;
        }
    }
}
