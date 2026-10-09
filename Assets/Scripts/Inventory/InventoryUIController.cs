using System.Collections.Generic;
using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    [SerializeField] private Transform barContainer;
    [SerializeField] private InventorySlotUI slotPrefab;

    private readonly Dictionary<WeaponData, InventorySlotUI> _slots = new Dictionary<WeaponData, InventorySlotUI>();

    private InventorySlotUI _selectedSlot;
    private int _lastKnownStage = -1;

    public WeaponData SelectedWeapon { get; private set; }
    public event System.Action<WeaponData> OnWeaponSelected;

    private void Start()
    {
        InventorySystem.Instance.OnCountChanged += HandleCountChanged;
        RefreshAll();
    }

    private void OnDisable()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnCountChanged -= HandleCountChanged;
    }

    private void Update()
    {
        int currentStage = StageManager.Instance != null ? StageManager.Instance.CurrentStageNumber : 1;
        if (currentStage != _lastKnownStage)
        {
            _lastKnownStage = currentStage;
            RefreshUnlockVisibility();
        }
    }

    private void RefreshAll()
    {
        foreach (var kvp in InventorySystem.Instance.GetAllCounts())
        {
            HandleCountChanged(kvp.Key, kvp.Value);
        }
    }

    private void HandleCountChanged(WeaponData data, int newCount)
    {
        if (!_slots.TryGetValue(data, out var slot))
        {
            slot = Instantiate(slotPrefab, barContainer);
            _slots[data] = slot;
            slot.gameObject.SetActive(IsUnlocked(data));
            slot.Setup(data, 0); // Setup with 0 first
            slot.UpdateCount(newCount); // Triggers the popup for new weapons!
            slot.OnSlotClicked += HandleSlotClicked;
        }
        else
        {
            slot.gameObject.SetActive(IsUnlocked(data));
            slot.UpdateCount(newCount);
        }
    }

    private void RefreshUnlockVisibility()
    {
        foreach (var kvp in _slots)
        {
            kvp.Value.gameObject.SetActive(IsUnlocked(kvp.Key));
        }
    }

    private bool IsUnlocked(WeaponData data)
    {
        if (InventorySystem.Instance == null || data == null) return false;

        // 1. If currently in stock (> 0), always show
        if (InventorySystem.Instance.GetCount(data) > 0)
            return true;

        // 2. In Tutorial: If count is 0, ONLY show if the player actually possessed it before!
        if (StageManager.Instance != null && StageManager.Instance.IsTutorialScene)
        {
            return InventorySystem.Instance.HasEverPossessed(data);
        }

        // 3. Main Campaign: Unlocked if stage reached AND was actually possessed before
        int currentStage = StageManager.Instance != null ? StageManager.Instance.CurrentStageNumber : 1;
        return data.unlockStage <= currentStage && InventorySystem.Instance.HasEverPossessed(data);
    }

    private void HandleSlotClicked(InventorySlotUI clickedSlot)
    {
        if (_selectedSlot == clickedSlot) return;

        if (_selectedSlot != null)
            _selectedSlot.SetSelected(false);

        _selectedSlot = clickedSlot;
        _selectedSlot.SetSelected(true);

        SelectedWeapon = clickedSlot.Data;
        OnWeaponSelected?.Invoke(SelectedWeapon);
    }
}