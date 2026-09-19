using System.Collections;
using UnityEngine;

/// <summary>
/// Big colorful blast animation on balloon pop.
/// Spawns: expanding ring, confetti shards, large particles, sparkle stars.
/// </summary>
public class PopEffect : MonoBehaviour
{
    [HideInInspector] public Sprite circleSprite;
    [HideInInspector] public int    particleCount = 12;
    [HideInInspector] public float  speed         = 5f;
    [HideInInspector] public float  lifetime      = 0.65f;
    [HideInInspector] public float  startScale    = 0.28f;

    void OnEnable()
    {
        Color baseColor = Color.white;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        StartCoroutine(BigBlast(baseColor));
    }

    IEnumerator BigBlast(Color baseColor)
    {
        Color.RGBToHSV(baseColor, out float baseH, out float baseS, out float baseV);

        // ── 1. Expanding shockwave ring ───────────────────────────────────
        var ring = MakeRing(baseColor);

        // ── 2. Large confetti shards (rectangles) ────────────────────────
        int shardCount = 10;
        var shards  = new GameObject[shardCount];
        var shardV  = new Vector3[shardCount];
        var shardSpin = new float[shardCount];
        for (int i = 0; i < shardCount; i++)
        {
            float angle = (360f / shardCount) * i + Random.Range(-18f, 18f);
            shardV[i]   = Quaternion.Euler(0, 0, angle) * Vector3.up
                          * Random.Range(speed * 0.7f, speed * 1.4f);
            shardSpin[i] = Random.Range(-360f, 360f);

            // Alternate colours around the hue wheel
            float hue = Mathf.Repeat(baseH + i * 0.12f, 1f);
            Color sc  = Color.HSVToRGB(hue, 0.95f, 1f);

            shards[i] = MakeShard(sc, transform.position,
                Random.Range(0.12f, 0.26f), Random.Range(0.06f, 0.14f));
        }

        // ── 3. Round particle burst (big) ────────────────────────────────
        var parts  = new GameObject[particleCount];
        var partV  = new Vector3[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            float angle = (360f / particleCount) * i + Random.Range(-15f, 15f);
            partV[i] = Quaternion.Euler(0, 0, angle) * Vector3.up
                       * Random.Range(speed * 0.5f, speed * 1.0f);

            float hue = Mathf.Repeat(baseH + i * 0.08f + 0.5f, 1f);
            Color pc  = Color.HSVToRGB(hue, 1f, 1f);

            var go = new GameObject("P");
            go.transform.position   = transform.position;
            go.transform.localScale = Vector3.one * startScale * Random.Range(1f, 2.2f);
            var psr = go.AddComponent<SpriteRenderer>();
            psr.sprite       = circleSprite;
            psr.color        = pc;
            psr.sortingOrder = 25;
            parts[i] = go;
        }

        // ── 4. Sparkle stars ─────────────────────────────────────────────
        int starCount = 6;
        var stars  = new GameObject[starCount];
        var starV  = new Vector3[starCount];
        for (int i = 0; i < starCount; i++)
        {
            float angle = Random.Range(0f, 360f);
            starV[i] = Quaternion.Euler(0, 0, angle) * Vector3.up
                       * Random.Range(speed * 1.0f, speed * 1.8f);

            var go = new GameObject("Star");
            go.transform.position   = transform.position;
            go.transform.localScale = Vector3.one * Random.Range(0.18f, 0.32f);
            var ssr = go.AddComponent<SpriteRenderer>();
            ssr.sprite       = MakeSparkle();
            ssr.color        = Color.white;
            ssr.sortingOrder = 26;
            stars[i] = go;
        }

        // ── Animate ───────────────────────────────────────────────────────
        float elapsed = 0f;
        while (elapsed < lifetime)
        {
            float t = elapsed / lifetime;
            float dt = Time.deltaTime;

            // Ring: expand and fade
            if (ring != null)
            {
                float rs = Mathf.Lerp(0.1f, 3.5f, t);
                ring.transform.localScale = Vector3.one * rs;
                var rsr = ring.GetComponent<SpriteRenderer>();
                if (rsr != null)
                {
                    Color rc = rsr.color; rc.a = Mathf.Clamp01((1f - t) * 2f);
                    rsr.color = rc;
                }
            }

            // Shards: fly out + spin + shrink + fade
            for (int i = 0; i < shardCount; i++)
            {
                if (shards[i] == null) continue;
                shards[i].transform.position += shardV[i] * dt;
                shardV[i] = Vector3.Lerp(shardV[i], Vector3.zero, dt * 3f);
                shards[i].transform.Rotate(0, 0, shardSpin[i] * dt);
                float ss = Mathf.Lerp(1f, 0.1f, t);
                shards[i].transform.localScale = new Vector3(
                    shards[i].transform.localScale.x * (1f - dt * 0.5f),
                    shards[i].transform.localScale.y * (1f - dt * 0.5f), 1f);
                var ssr2 = shards[i].GetComponent<SpriteRenderer>();
                if (ssr2 != null)
                { Color c = ssr2.color; c.a = 1f - t; ssr2.color = c; }
            }

            // Round particles: fly + fade + shrink
            for (int i = 0; i < particleCount; i++)
            {
                if (parts[i] == null) continue;
                parts[i].transform.position += partV[i] * dt;
                partV[i] = Vector3.Lerp(partV[i], Vector3.zero, dt * 4f);
                float ps = Mathf.Lerp(1f, 0.2f, t);
                parts[i].transform.localScale *= (1f - dt * 1.5f);
                var psr2 = parts[i].GetComponent<SpriteRenderer>();
                if (psr2 != null)
                { Color c = psr2.color; c.a = 1f - t; psr2.color = c; }
            }

            // Stars: fast fly + twinkle
            for (int i = 0; i < starCount; i++)
            {
                if (stars[i] == null) continue;
                stars[i].transform.position += starV[i] * dt;
                starV[i] = Vector3.Lerp(starV[i], Vector3.zero, dt * 3.5f);
                stars[i].transform.Rotate(0, 0, 180f * dt);
                float sa = t < 0.4f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.4f) / 0.6f);
                var srs = stars[i].GetComponent<SpriteRenderer>();
                if (srs != null) { Color c = srs.color; c.a = sa; srs.color = c; }
            }

            elapsed += dt;
            yield return null;
        }

        // Cleanup
        if (ring) Destroy(ring);
        foreach (var g in shards)  if (g) Destroy(g);
        foreach (var g in parts)   if (g) Destroy(g);
        foreach (var g in stars)   if (g) Destroy(g);
        Destroy(gameObject);
    }

    // ── Factory helpers ───────────────────────────────────────────────────

    GameObject MakeRing(Color col)
    {
        int size = 64;
        var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color[size * size];
        float cx = size * 0.5f, cy = size * 0.5f;
        float outerR = size * 0.46f, innerR = size * 0.38f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Mathf.Sqrt((x-cx)*(x-cx)+(y-cy)*(y-cy));
            bool inRing = d >= innerR && d <= outerR;
            px[y*size+x] = inRing ? new Color(col.r, col.g, col.b, 0.9f) : Color.clear;
        }
        tex.SetPixels(px); tex.Apply();

        var spr = Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), size);
        var go  = new GameObject("Ring");
        go.transform.position = transform.position;
        var sr  = go.AddComponent<SpriteRenderer>();
        sr.sprite       = spr;
        sr.color        = col;
        sr.sortingOrder = 22;
        return go;
    }

    GameObject MakeShard(Color col, Vector3 pos, float w, float h)
    {
        var tex = new Texture2D(16, 8, TextureFormat.RGBA32, false);
        var px  = new Color[128];
        for (int i = 0; i < 128; i++) px[i] = col;
        tex.SetPixels(px); tex.Apply();
        var spr = Sprite.Create(tex, new Rect(0,0,16,8), new Vector2(0.5f,0.5f), 16f);

        var go = new GameObject("Shard");
        go.transform.position   = pos;
        go.transform.localScale = new Vector3(w, h, 1f);
        go.transform.rotation   = Quaternion.Euler(0, 0, Random.Range(0, 360));
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = spr;
        sr.color        = col;
        sr.sortingOrder = 23;
        return go;
    }

    Sprite MakeSparkle()
    {
        int size = 32;
        var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var px   = new Color[size * size];
        float cx = size * 0.5f, cy = size * 0.5f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - cx, dy = y - cy;
            // 4-pointed star shape
            float dist  = Mathf.Sqrt(dx*dx + dy*dy);
            float angle = Mathf.Atan2(Mathf.Abs(dy), Mathf.Abs(dx));
            float spike = Mathf.Cos(angle * 2f);
            float r     = size * 0.46f * Mathf.Lerp(0.15f, 1f, spike * spike);
            float a     = Mathf.Clamp01((r - dist) / 3f);
            px[y*size+x] = new Color(1f, 1f, 0.8f, a);
        }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), size);
    }
}
