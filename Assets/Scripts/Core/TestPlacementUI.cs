using UnityEngine;
using UnityEngine.UI;

// Sementara: tombol UI buat manual-trigger PlaceFromInventory(),
// menggantikan drag-drop asli yang belum ada. Hapus/replace script ini
// begitu sistem drag-drop beneran udah jadi.
public class TestPlacementUI : MonoBehaviour
{
    [SerializeField] private Button placeButton;
    [SerializeField] private WeaponData weaponToPlace;
    [SerializeField] private Vector2Int gridOrigin;

    private void Start()
    {
        placeButton.onClick.AddListener(OnPlaceButtonClicked);
    }

    private void OnPlaceButtonClicked()
    {
        var result = InventorySystem.Instance.PlaceFromInventory(weaponToPlace, gridOrigin);
        if (result == null)
            Debug.Log("Placement gagal (slot penuh / inventory habis / stage sudah berakhir).");
        else
            Debug.Log($"Placement berhasil: {weaponToPlace.name} di {gridOrigin}");
    }
}