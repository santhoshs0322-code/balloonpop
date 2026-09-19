using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fades a dark overlay over the background during gameplay
/// so balloons stand out clearly against the sky.
/// </summary>
public class BackgroundDimmer : MonoBehaviour
{
    [HideInInspector] public Image overlayImage;
    [HideInInspector] public float dimAmount = 0.35f;

    private Coroutine _fade;

    void OnEnable()
    {
        GameManager.OnGameStart += DimOn;
        GameManager.OnGameOver  += DimOff;
        GameManager.OnGoToMenu  += DimOff;
    }

    void OnDisable()
    {
        GameManager.OnGameStart -= DimOn;
        GameManager.OnGameOver  -= DimOff;
        GameManager.OnGoToMenu  -= DimOff;
    }

    void DimOn()  => Fade(dimAmount);
    void DimOff() => Fade(0f);

    void Fade(float target)
    {
        if (overlayImage == null) return;
        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(DoFade(target));
    }

    IEnumerator DoFade(float target)
    {
        float start = overlayImage.color.a;
        float t = 0f;
        while (t < 0.45f)
        {
            overlayImage.color = new Color(0f, 0f, 0f,
                Mathf.Lerp(start, target, t / 0.45f));
            t += Time.deltaTime;
            yield return null;
        }
        overlayImage.color = new Color(0f, 0f, 0f, target);
    }
}
