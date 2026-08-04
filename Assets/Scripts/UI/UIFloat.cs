using UnityEngine;
using UnityEngine.UI;

public class UIFloat : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float verticalDistance = 10f;
    [SerializeField] private bool infinityPath = false;
    [SerializeField] private float horizontalDistance = 20f;

    [Header("Alpha")]
    [SerializeField] private bool pulseAlpha = false;
    [Range(0, 1)]
    [SerializeField] private float minAlpha = 0.5f;

    private Vector3 originalPosition;
    private Color originalColor;
    private Graphic uiGraphic;
    private SpriteRenderer sprite2D;

    private void Awake()
    {
        originalPosition = transform.localPosition;

        uiGraphic = GetComponent<Graphic>();
        if (uiGraphic != null)
        {
            originalColor = uiGraphic.color;
            originalColor.a = 1f;
        }

        sprite2D = GetComponent<SpriteRenderer>();
        if (sprite2D != null)
        {
            originalColor = sprite2D.color;
            originalColor.a = 1f;
        }
    }

    private void Update()
    {
        float time = Time.time * moveSpeed;

        Vector3 offset = Vector3.zero;

        // Vertical movement
        offset.y = Mathf.Sin(time) * verticalDistance;

        if (infinityPath)
        {
            // infinity
            offset.x = Mathf.Sin(time) * horizontalDistance;
            offset.y = Mathf.Sin(time * 2f) * verticalDistance;
        }
        else
        {
            // Simple up/down bob
            offset.y = Mathf.Sin(time) * verticalDistance;
        }

        transform.localPosition = originalPosition + offset;

        if (pulseAlpha)
        {
            float alpha = Mathf.Lerp(minAlpha, 1f, (Mathf.Sin(time) + 1f) * 0.5f);

            if (uiGraphic != null)
            {
                uiGraphic.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            }
            else if (sprite2D != null)
            {
                sprite2D.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            }
        }
    }

    private void OnEnable()
    {
        transform.localPosition = originalPosition;

        if (uiGraphic != null)
        {
            uiGraphic.color = originalColor;
        }

        if (sprite2D != null)
        {
            sprite2D.color = originalColor;
        }
    }
}