using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Floating "+10" popup using built-in UI.Text.
/// Renamed to ScorePopupLegacy to match UIManager reference.
/// </summary>
public class ScorePopupLegacy : MonoBehaviour
{
    public float riseDistance = 90f;
    public float duration     = 0.85f;

    void Start() => StartCoroutine(Animate());

    IEnumerator Animate()
    {
        var txt = GetComponent<Text>();
        var rt  = GetComponent<RectTransform>();
        Vector2 startPos  = rt.localPosition;
        Color   startCol  = txt != null ? txt.color : Color.white;
        float   elapsed   = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            rt.localPosition = startPos + Vector2.up * riseDistance * t;

            if (txt != null)
            {
                // Punch scale at start, then fade
                float scale = t < 0.2f ? Mathf.Lerp(1.35f, 1f, t / 0.2f) : 1f;
                rt.localScale = Vector3.one * scale;

                Color c = startCol;
                c.a = 1f - t;
                txt.color = c;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}

// Keep the old class name as alias so nothing breaks if referenced elsewhere
public class ScorePopup : ScorePopupLegacy { }
