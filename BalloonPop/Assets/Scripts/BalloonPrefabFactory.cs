using UnityEngine;

/// <summary>
/// Builds the Balloon prefab entirely in code using SpriteFactory shapes.
/// Reverted to original working version.
/// </summary>
public static class BalloonPrefabFactory
{
    public static GameObject Build()
    {
        GameObject go = new GameObject("BalloonTemplate");
        go.SetActive(false);

        SpriteRenderer sr  = go.AddComponent<SpriteRenderer>();
        sr.sprite          = SpriteFactory.MakeBalloon(128, Color.white);
        sr.sortingOrder    = 5;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = false;
        col.radius    = 0.44f;

        go.transform.localScale = Vector3.one * 0.72f;

        Balloon b = go.AddComponent<Balloon>();

        b.typeSprites = new Sprite[]
        {
            SpriteFactory.MakeBalloon     (128, new Color(0.95f, 0.18f, 0.18f)),
            SpriteFactory.MakeBalloon     (128, new Color(0.20f, 0.45f, 1.00f)),
            SpriteFactory.MakeBalloon     (128, new Color(0.15f, 0.82f, 0.22f)),
            SpriteFactory.MakeStar        (128, new Color(1.00f, 0.82f, 0.05f)),
            SpriteFactory.MakeRainbowCircle(128),
            SpriteFactory.MakeBomb        (128),
            SpriteFactory.MakeHeartBalloon(128, new Color(1f, 0.32f, 0.55f)),
        };

        b.typeColors = new Color[]
        {
            new Color(0.95f, 0.18f, 0.18f),
            new Color(0.20f, 0.45f, 1.00f),
            new Color(0.15f, 0.82f, 0.22f),
            new Color(1.00f, 0.82f, 0.05f),
            new Color(0.85f, 0.40f, 1.00f),
            new Color(0.20f, 0.20f, 0.20f),
            new Color(1.00f, 0.32f, 0.55f),
        };

        b.popEffectPrefab = BuildPopEffect();
        return go;
    }

    static GameObject BuildPopEffect()
    {
        GameObject go = new GameObject("PopEffect");
        go.SetActive(false);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite         = SpriteFactory.MakeCircleGloss(64, Color.white, Color.white);
        sr.enabled        = false;

        PopEffect pe     = go.AddComponent<PopEffect>();
        pe.circleSprite  = SpriteFactory.MakeCircleGloss(32, Color.white, Color.white);
        pe.particleCount = 12;
        pe.speed         = 4.5f;
        pe.lifetime      = 0.55f;
        pe.startScale    = 0.22f;

        return go;
    }
}
