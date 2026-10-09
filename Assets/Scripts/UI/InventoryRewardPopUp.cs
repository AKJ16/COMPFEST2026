using TMPro;
using UnityEngine;

public class InventoryRewardPopUp : MonoBehaviour
{
    [SerializeField] private float speed = 50f;
    [SerializeField] private float lifetime = 1.2f;
    [SerializeField] private TextMeshProUGUI text;

    private RectTransform rectTransform;
    private Color baseColor;
    private float timer;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (text == null) text = GetComponent<TextMeshProUGUI>();
        if (text != null) baseColor = text.color;
    }

    public void Setup(int amount)
    {
        if (text == null) text = GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = $"+{amount}";
            baseColor = text.color;
        }
    }

    private void Update()
    {
        // Moves upward in Canvas UI pixels
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition += Vector2.up * (speed * Time.unscaledDeltaTime);
        }

        timer += Time.unscaledDeltaTime;

        if (text != null)
        {
            float alpha = Mathf.Lerp(1f, 0f, timer / lifetime);
            text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}