using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPulse : MonoBehaviour
{
    // Attributes

    [SerializeField] private float pulseSpeed = 6f;
    [SerializeField] private float scaleAmount = 0.05f;
    [SerializeField] private bool pulseAlpha = false;
    [Range(0, 1)][SerializeField] private float minAlpha = 0.5f;

    private Vector3 originalScale;
    private Color originalColor;
    private Graphic uiGraphic;
    private SpriteRenderer sprite2D;

    // Methods

    private void Awake()
    {
        originalScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
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
        float time = Time.unscaledTime * pulseSpeed;
        float sin = Mathf.Sin(time);

        float scaleMultiplier = 1f + (sin * scaleAmount);
        transform.localScale = originalScale * scaleMultiplier;

        if (pulseAlpha)
        {
            float alpha = Mathf.Lerp(minAlpha, 1f, (sin + 1f) / 2f);

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
        transform.localScale = originalScale;
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
