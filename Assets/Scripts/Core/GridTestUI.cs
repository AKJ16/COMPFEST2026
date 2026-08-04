using UnityEngine;
using UnityEngine.UI;

// TEST SCAFFOLD ONLY — bukan Grid System beneran (itu jatah Adriel).
// Ini cuma buat Darryl bisa test InventorySystem/WeaponGridManager/WeaponEffectsSystem
// sendiri tanpa nunggu drag & drop asli jadi. Boleh dihapus begitu Adriel punya
// placement system sendiri.
//
// Cara pakai: taro GameObject kosong di dalam Canvas (sejajar InventoryBar),
// attach script ini, drag InventoryUIController ke field-nya, lalu Play.
public class GridTestUI : MonoBehaviour
{
    [SerializeField] private InventoryUIController inventoryUI;
    [SerializeField] private float cellPixelSize = 48f;
    [SerializeField] private float cellSpacing = 4f;

    private void Start()
    {
        BuildGrid();
    }

    private void BuildGrid()
    {
        int width = WeaponGridManager.Instance.GridWidth;
        int height = WeaponGridManager.Instance.GridHeight;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                CreateCellButton(x, y);
            }
        }
    }

    private void CreateCellButton(int x, int y)
    {
        var go = new GameObject($"Cell_{x}_{y}", typeof(RectTransform));
        go.transform.SetParent(transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(cellPixelSize, cellPixelSize);
        rect.anchoredPosition = new Vector2(
            x * (cellPixelSize + cellSpacing),
            y * (cellPixelSize + cellSpacing));

        var image = go.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.15f); // kotak transparan, cuma buat keliatan area klik-nya

        var button = go.AddComponent<Button>();

        // Capture x,y lokal biar closure-nya bener per tombol.
        int capturedX = x;
        int capturedY = y;
        button.onClick.AddListener(() => TryPlaceAt(capturedX, capturedY));
    }

    private void TryPlaceAt(int x, int y)
    {
        var selected = inventoryUI.SelectedWeapon;
        if (selected == null)
        {
            Debug.LogWarning("GridTestUI: belum ada weapon yang dipilih di inventory.");
            return;
        }

        var result = InventorySystem.Instance.PlaceFromInventory(selected, new Vector2Int(x, y));
        if (result == null)
            Debug.LogWarning($"GridTestUI: gagal taro {selected.weaponName} di ({x},{y}) — cell penuh/invalid atau stage sudah berakhir.");
        else
            Debug.Log($"GridTestUI: {selected.weaponName} ditaro di ({x},{y}), sequence #{result.SequenceIndex}.");
    }
}
