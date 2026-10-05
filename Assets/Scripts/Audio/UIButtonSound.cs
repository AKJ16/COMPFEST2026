using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    [Tooltip("Optional custom sound. If left empty, plays AudioManager's default buttonClickSfx.")]
    [SerializeField] private AudioClip customClickSfx;

    private void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(PlaySound);
        }
    }

    private void PlaySound()
    {
        if (AudioManager.Instance == null) return;

        if (customClickSfx != null)
        {
            AudioManager.Instance.PlaySFX(customClickSfx);
        }
        else
        {
            AudioManager.Instance.PlayButtonClick();
        }
    }
}