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

            // 1. Activate slot FIRST so it is active in hierarchy if unlocked
            slot.gameObject.SetActive(IsUnlocked(data));

            // 2. Setup initial slot data with count = 0
            slot.Setup(data, 0);

            // 3. Update count (triggers popup on Stage 2+ for newly unlocked weapons)
            slot.UpdateCount(newCount);

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
        int currentStage = StageManager.Instance != null ? StageManager.Instance.CurrentStageNumber : 1;
        return data.unlockStage <= currentStage;
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