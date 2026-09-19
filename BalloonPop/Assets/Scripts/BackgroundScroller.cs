using UnityEngine;

/// <summary>
/// Optional: slowly scrolls a background sprite upward for a sky/cloud feel.
/// Attach to your background SpriteRenderer GameObject.
/// Set the sprite to a seamless tiling sky texture, or leave as a solid color.
/// </summary>
public class BackgroundScroller : MonoBehaviour
{
    [Tooltip("Scroll speed (world units per second).")]
    public float scrollSpeed = 0.1f;

    [Tooltip("Y position to reset (loop) the background.")]
    public float resetY = -10f;

    [Tooltip("Y position at which scroll resets.")]
    public float startY = 10f;

    private Vector3 _startPosition;

    void Start()
    {
        _startPosition = transform.position;
    }

    void Update()
    {
        transform.position += Vector3.down * scrollSpeed * Time.deltaTime;

        if (transform.position.y < resetY)
            transform.position = new Vector3(transform.position.x, startY, transform.position.z);
    }
}
