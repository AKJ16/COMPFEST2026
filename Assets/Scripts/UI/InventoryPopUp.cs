using TMPro;
using UnityEngine;

public class InventoryRewardPopUp : MonoBehaviour
{
    [SerializeField] private float speed = 40f;
    [SerializeField] private float lifetime = 1.2f;
    [SerializeField] private Color popUpColor = new Color(0.2f, 1f, 0.3f, 1f); // Toxic/Bright Green

    private TextMeshProUGUI text;
    private float timer;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 50;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }
        text.color = popUpColor;
    }

    public void Setup(int amount)
    {
        if (text == null) Awake();
        text.text = $"+{amount}";
        text.color = popUpColor;
    }

    private void Update()
    {
        transform.Translate(Vector3.up * speed * Time.deltaTime);

        timer += Time.deltaTime;

        float alpha = Mathf.Lerp(1f, 0f, timer / lifetime);
        text.color = new Color(popUpColor.r, popUpColor.g, popUpColor.b, alpha);

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}