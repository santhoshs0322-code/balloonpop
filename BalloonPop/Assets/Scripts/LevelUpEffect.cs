using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animates the level-up banner using built-in UI.Text (no TMP needed).
/// </summary>
public class LevelUpEffect : MonoBehaviour
{
    // Set by UIManager at build time
    [HideInInspector] public Text levelUpTextLegacy;

    public float displayDuration = 1.6f;

    void OnEnable()  => GameManager.OnLevelChanged += OnLevelChanged;
    void OnDisable() => GameManager.OnLevelChanged -= OnLevelChanged;

    void OnLevelChanged(int level)
    {
        if (level <= 1) return;
        StopAllCoroutines();
        StartCoroutine(Animate(level));
    }

    IEnumerator Animate(int level)
    {
        gameObject.SetActive(true);
        if (levelUpTextLegacy != null) levelUpTextLegacy.text = "LEVEL " + level + "!";

        float elapsed = 0f;
        // Scale in
        while (elapsed < 0.25f)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.15f, elapsed / 0.25f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = Vector3.one;

        yield return new WaitForSeconds(displayDuration - 0.5f);

        // Fade out
        elapsed = 0f;
        Color startCol = levelUpTextLegacy != null ? levelUpTextLegacy.color : Color.white;
        while (elapsed < 0.25f)
        {
            float t = elapsed / 0.25f;
            if (levelUpTextLegacy != null)
            {
                Color c = startCol; c.a = 1f - t;
                levelUpTextLegacy.color = c;
            }
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (levelUpTextLegacy != null)
        {
            Color c = startCol; c.a = 1f;
            levelUpTextLegacy.color = c;
        }
        gameObject.SetActive(false);
    }
}
