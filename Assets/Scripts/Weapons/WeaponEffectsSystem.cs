using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeaponEffectsSystem : MonoBehaviour
{
    public static WeaponEffectsSystem Instance { get; private set; }

    /// <summary>
    /// GLOBAL STATE: True while ANY weapon attack, particle effect, or Hourglass chain is actively running.
    /// </summary>
    public static bool IsBusy { get; private set; } = false;

    [SerializeField] private EnemyHealth currentEnemy;

    private void Awake()
    {
        Instance = this;
    }

    public void StartStage(EnemyHealth enemy)
    {
        currentEnemy = enemy;
        IsBusy = false;
    }

    public void ResolvePlacement(WeaponInstance instance)
    {
        StartCoroutine(ResolvePlacementRoutine(instance));
    }

    private IEnumerator ResolvePlacementRoutine(WeaponInstance instance)
    {
        // 1. HARD LOCK: An attack sequence has started!
        IsBusy = true;

        // 2. Tick existing poison at the start of placement turn
        if (currentEnemy != null)
        {
            currentEnemy.TickPoisonTurn();
        }

        // 3. Resolve placement effects based on category
        switch (instance.Data.category)
        {
            case WeaponCategory.Attack:
                ResolveAttack(instance);
                yield return new WaitForSeconds(0.35f);
                break;

            case WeaponCategory.Modifier:
                if (instance.Data.modifierType == ModifierType.Repeat)
                {
                    yield return StartCoroutine(HourglassChainRoutine(instance));
                }
                else
                {
                    PlayAttackFeedback(instance.Data);
                    yield return new WaitForSeconds(0.25f);
                }
                break;

            case WeaponCategory.Utility:
                PlayAttackFeedback(instance.Data);
                yield return new WaitForSeconds(0.20f);
                break;
        }

        yield return new WaitForSeconds(0.15f);

        // 4. UNLOCK: All animations and chains are 100% finished!
        IsBusy = false;

        // 5. Safely check if stage ended or player lost
        if (StageManager.Instance != null)
        {
            StageManager.Instance.CheckForEndOfStage();
        }
    }

    private void ResolveAttack(WeaponInstance attackInstance)
    {
        float multiplier = 1f;
        float addition = 0f;

        var adjacentModifiers = WeaponGridManager.Instance.GetNeighborsOf(attackInstance)
            .Where(n => n.Data.category == WeaponCategory.Modifier && n.Data.modifierType != ModifierType.Repeat);

        var boostingBooks = new List<WeaponInstance>();

        foreach (var modifier in adjacentModifiers)
        {
            if (modifier.Data.targets != null && modifier.Data.targets.Contains(attackInstance.Data))
            {
                switch (modifier.Data.modifierType)
                {
                    case ModifierType.Multiplier:
                        multiplier *= modifier.Data.modifierValue;
                        if (!Mathf.Approximately(modifier.Data.modifierValue, 1f))
                            boostingBooks.Add(modifier);
                        break;

                    case ModifierType.Addition:
                        addition += modifier.Data.modifierValue;
                        if (!Mathf.Approximately(modifier.Data.modifierValue, 0f))
                            boostingBooks.Add(modifier);
                        break;
                }
            }
        }

        int finalDamage = Mathf.RoundToInt((attackInstance.Data.baseDamage * multiplier) + addition);
        attackInstance.ResolvedDamage = finalDamage;

        if (currentEnemy != null)
        {
            currentEnemy.TakeDamage(finalDamage);

            if (attackInstance.Data.appliesPoison)
                currentEnemy.ApplyPoison(attackInstance, attackInstance.Data.poisonDamagePerTick);

            // Teammate's Handbook Combo Logger
            foreach (var book in boostingBooks)
            {
                HandbookUI.ReportCombo(book.Data, attackInstance.Data,
                    BuildBookComboDetail(book.Data, attackInstance.Data, boostingBooks.Count, finalDamage));
            }
        }

        PlayAttackFeedback(attackInstance.Data);
    }

    private static string BuildBookComboDetail(WeaponData book, WeaponData target, int bookCount, int finalDamage)
    {
        int baseDamage = target.baseDamage;
        float v = book.modifierValue;

        string detail = book.modifierType == ModifierType.Multiplier
            ? $"{target.weaponName} damage went from {baseDamage} to {Mathf.RoundToInt(baseDamage * v)} (x{v:0.##})"
            : $"{target.weaponName} damage went from {baseDamage} to {Mathf.RoundToInt(baseDamage + v)} (+{v:0.##})";

        if (bookCount > 1)
            detail += $". With every book in range the hit was {finalDamage}";

        return detail + ".";
    }

    private void PlayAttackFeedback(WeaponData data)
    {
        if (data == null) return;

        Vector3 feedbackPosition = currentEnemy != null ? currentEnemy.transform.position : transform.position;

        if (data.attackSfx != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(data.attackSfx);

        if (data.attackVfxPrefab != null && VFXManager.Instance != null)
            VFXManager.Instance.PlayWeaponEffect(data.attackVfxPrefab, feedbackPosition);
    }

    private IEnumerator HourglassChainRoutine(WeaponInstance initialHourglass)
    {
        var visitedHourglasses = new HashSet<WeaponInstance>();
        var queue = new Queue<WeaponInstance>();

        visitedHourglasses.Add(initialHourglass);
        queue.Enqueue(initialHourglass);

        while (queue.Count > 0)
        {
            var currentHourglass = queue.Dequeue();

            // 1. Play Hourglass SFX & Spin animation
            PlayAttackFeedback(currentHourglass.Data);
            if (currentHourglass.VisualObject != null)
            {
                currentHourglass.VisualObject.PlayAnimation();
            }

            // 2. Wait for Hourglass spin to complete
            yield return new WaitForSeconds(0.40f);

            // 3. Find all neighbors (8-directional)
            var allNeighbors = WeaponGridManager.Instance.GetNeighborsOf(currentHourglass).ToList();

            // A. Trigger adjacent Attack weapons SEQUENTIALLY
            var attackNeighbors = allNeighbors.Where(n => n.Data.category == WeaponCategory.Attack);
            foreach (var neighbor in attackNeighbors)
            {
                if (currentEnemy != null)
                {
                    currentEnemy.TakeDamage(neighbor.ResolvedDamage);

                    string detail = $"Replayed {neighbor.Data.weaponName}: {neighbor.ResolvedDamage} damage dealt again";

                    if (neighbor.Data.appliesPoison)
                    {
                        currentEnemy.ApplyPoison(neighbor, neighbor.Data.poisonDamagePerTick);
                        detail += ", and its poison was applied again";
                    }

                    // Teammate's Handbook Combo Logger
                    HandbookUI.ReportCombo(currentHourglass.Data, neighbor.Data, detail + ".");
                }

                PlayAttackFeedback(neighbor.Data);

                if (neighbor.VisualObject != null)
                {
                    neighbor.VisualObject.PlayAnimation();
                }

                // Wait for individual weapon animation before next weapon strikes
                yield return new WaitForSeconds(0.35f);

                if (currentEnemy != null && currentEnemy.State == EnemyState.Dead)
                {
                    yield break;
                }
            }

            // B. Chain Reaction: Queue adjacent Hourglasses
            var adjacentHourglasses = allNeighbors.Where(n => n.Data.modifierType == ModifierType.Repeat);
            foreach (var hNeighbor in adjacentHourglasses)
            {
                if (!visitedHourglasses.Contains(hNeighbor))
                {
                    visitedHourglasses.Add(hNeighbor);
                    queue.Enqueue(hNeighbor);
                }
            }

            if (queue.Count > 0)
            {
                yield return new WaitForSeconds(0.15f);
            }
        }

        yield return new WaitForSeconds(0.20f);
    }
}