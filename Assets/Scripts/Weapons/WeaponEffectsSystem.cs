using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeaponEffectsSystem : MonoBehaviour
{
    public static WeaponEffectsSystem Instance { get; private set; }

    /// <summary>
    /// True ONLY while an Hourglass rewind or Hourglass chain reaction is actively running.
    /// Dragging is locked only when this is true.
    /// </summary>
    public static bool IsHourglassBusy { get; private set; } = false;

    // Backwards-compatible alias for other scripts
    public static bool IsBusy => IsHourglassBusy;

    [SerializeField] private EnemyHealth currentEnemy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartStage(EnemyHealth enemy)
    {
        currentEnemy = enemy;
        IsHourglassBusy = false;
    }

    public void ResolvePlacement(WeaponInstance instance)
    {
        if (instance == null || instance.Data == null)
            return;

        StartCoroutine(ResolvePlacementRoutine(instance));
    }

    private IEnumerator ResolvePlacementRoutine(WeaponInstance instance)
    {
        // 1. TICK EXISTING POISON
        if (currentEnemy != null)
        {
            currentEnemy.TickPoisonTurn();
        }

        // 2. RESOLVE WEAPON
        switch (instance.Data.category)
        {
            case WeaponCategory.Attack:
                ResolveAttack(instance);
                yield return new WaitForSeconds(0.20f);
                break;

            case WeaponCategory.Modifier:
                if (instance.Data.modifierType == ModifierType.Repeat)
                {
                    // LOCK DRAGGING ONLY DURING HOURGLASS REWIND
                    IsHourglassBusy = true;

                    if (VFXManager.Instance != null)
                    {
                        VFXManager.Instance.BeginHourglassRewind();
                    }

                    yield return StartCoroutine(HourglassChainRoutine(instance));

                    if (VFXManager.Instance != null)
                    {
                        VFXManager.Instance.EndHourglassRewind();
                    }

                    // UNLOCK DRAGGING
                    IsHourglassBusy = false;
                }
                else
                {
                    PlayBookScreenFeedback(instance.Data);
                    PlayAttackFeedback(instance.Data);
                    yield return new WaitForSeconds(0.15f);
                }
                break;

            case WeaponCategory.Utility:
                PlayAttackFeedback(instance.Data);
                yield return new WaitForSeconds(0.15f);
                break;
        }

        // 3. CHECK STAGE (Only if no Hourglass chain is running)
        if (StageManager.Instance != null && !IsHourglassBusy)
        {
            StageManager.Instance.CheckForEndOfStage();
        }
    }

    private void ResolveAttack(WeaponInstance attackInstance)
    {
        if (attackInstance == null || attackInstance.Data == null)
            return;

        float multiplier = 1f;
        float addition = 0f;

        var adjacentModifiers = WeaponGridManager.Instance
            .GetNeighborsOf(attackInstance)
            .Where(n => n != null && n.Data != null &&
                        n.Data.category == WeaponCategory.Modifier &&
                        n.Data.modifierType != ModifierType.Repeat);

        var boostingBooks = new List<WeaponInstance>();

        foreach (var modifier in adjacentModifiers)
        {
            if (modifier.Data.targets == null || !modifier.Data.targets.Contains(attackInstance.Data))
                continue;

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

        int finalDamage = Mathf.RoundToInt((attackInstance.Data.baseDamage * multiplier) + addition);
        attackInstance.ResolvedDamage = finalDamage;

        if (currentEnemy != null)
        {
            currentEnemy.TakeDamage(finalDamage);

            if (attackInstance.Data.appliesPoison)
            {
                currentEnemy.ApplyPoison(attackInstance, attackInstance.Data.poisonDamagePerTick);
            }

            foreach (var book in boostingBooks)
            {
                HandbookUI.ReportCombo(
                    book.Data,
                    attackInstance.Data,
                    BuildBookComboDetail(book.Data, attackInstance.Data, boostingBooks.Count, finalDamage)
                );
            }
        }

        PlayAttackFeedback(attackInstance.Data);
    }

    private static string BuildBookComboDetail(WeaponData book, WeaponData target, int bookCount, int finalDamage)
    {
        int baseDamage = target.baseDamage;
        float value = book.modifierValue;

        string detail = book.modifierType == ModifierType.Multiplier
            ? $"{target.weaponName} damage went from {baseDamage} to {Mathf.RoundToInt(baseDamage * value)} (x{value:0.##})"
            : $"{target.weaponName} damage went from {baseDamage} to {Mathf.RoundToInt(baseDamage + value)} (+{value:0.##})";

        if (bookCount > 1)
        {
            detail += $". With every book in range the hit was {finalDamage}";
        }

        return detail + ".";
    }

    private void PlayBookScreenFeedback(WeaponData data)
    {
        if (data == null || VFXManager.Instance == null)
            return;

        if (data.modifierType == ModifierType.Addition)
        {
            VFXManager.Instance.PlayAdditionScreenEffect();
        }
        else if (data.modifierType == ModifierType.Multiplier)
        {
            VFXManager.Instance.PlayMultiplicationScreenEffect();
        }
        else if (data.modifierType == ModifierType.Repeat)
        {
            VFXManager.Instance.PlayHourglassScreenEffect();
        }
    }

    private void PlayAttackFeedback(WeaponData data)
    {
        if (data == null)
            return;

        Vector3 feedbackPosition = currentEnemy != null
            ? currentEnemy.transform.position
            : transform.position;

        feedbackPosition.z = 0f;

        if (data.attackSfx != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(data.attackSfx);
        }

        string weaponName = !string.IsNullOrEmpty(data.weaponName)
            ? data.weaponName.ToLower()
            : (data.name != null ? data.name.ToLower() : "");

        if (weaponName.Contains("sword") || weaponName.Contains("pedang") || weaponName.Contains("blade"))
        {
            WeaponVfxSword.Play(feedbackPosition);
            return;
        }

        if (weaponName.Contains("poison") || weaponName.Contains("dagger") || weaponName.Contains("knife") || data.appliesPoison)
        {
            WeaponVfxPoison.Play(feedbackPosition);
            return;
        }

        if (weaponName.Contains("staff") || weaponName.Contains("tongkat") || weaponName.Contains("wand"))
        {
            WeaponVfxStaff.Play(feedbackPosition);
            return;
        }

        if (weaponName.Contains("hourglass") || weaponName.Contains("glass") || data.modifierType == ModifierType.Repeat)
        {
            WeaponVfxHourglass.Play(feedbackPosition);
            return;
        }

        if (data.modifierType == ModifierType.Addition || weaponName.Contains("addition") || weaponName.Contains("add"))
        {
            WeaponVfxBooks.PlayAddition(feedbackPosition);
            return;
        }

        if (data.modifierType == ModifierType.Multiplier || weaponName.Contains("multi") || weaponName.Contains("multiply"))
        {
            WeaponVfxBooks.PlayMultiplier(feedbackPosition);
            return;
        }

        if (WeaponVfx.Instance != null)
        {
            WeaponVfx.Instance.Play(data, feedbackPosition);
        }
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

            // 1. Play Hourglass SFX & Spin animation ONCE
            PlayAttackFeedback(currentHourglass.Data);
            if (currentHourglass.VisualObject != null)
            {
                currentHourglass.VisualObject.PlayAnimation();
            }

            // Snappy spin wait (reduced from 1.10s down to 0.30s)
            yield return new WaitForSeconds(0.30f);

            // 2. Find neighbors
            var allNeighbors = WeaponGridManager.Instance
                .GetNeighborsOf(currentHourglass)
                .ToList();

            // 3. Replay attack weapons sequentially with snappy delay
            var attackNeighbors = allNeighbors.Where(
                n => n != null && n.Data != null &&
                     n.Data.category == WeaponCategory.Attack &&
                     n.Data.modifierType != ModifierType.Repeat &&
                     !visitedHourglasses.Contains(n)
            );

            foreach (var neighbor in attackNeighbors)
            {
                int replayDamage = neighbor.ResolvedDamage;

                if (currentEnemy != null)
                {
                    currentEnemy.TakeDamage(replayDamage);

                    string detail = $"Replayed {neighbor.Data.weaponName}: {neighbor.ResolvedDamage} damage dealt again";

                    if (neighbor.Data.appliesPoison)
                    {
                        currentEnemy.ApplyPoison(neighbor, neighbor.Data.poisonDamagePerTick);
                        detail += ", and its poison was applied again";
                    }

                    HandbookUI.ReportCombo(currentHourglass.Data, neighbor.Data, detail + ".");
                }

                PlayAttackFeedback(neighbor.Data);

                if (neighbor.VisualObject != null)
                {
                    neighbor.VisualObject.PlayAnimation();
                }

                // Snappy delay between sequential weapon attacks (reduced to 0.22s)
                yield return new WaitForSeconds(0.22f);

                if (currentEnemy != null && currentEnemy.State == EnemyState.Dead)
                {
                    yield break;
                }
            }

            // 4. Queue other Hourglasses
            var adjacentHourglasses = allNeighbors.Where(
                n => n != null && n.Data != null &&
                     n.Data.modifierType == ModifierType.Repeat &&
                     !visitedHourglasses.Contains(n)
            );

            foreach (var hNeighbor in adjacentHourglasses)
            {
                visitedHourglasses.Add(hNeighbor);

                HandbookUI.ReportCombo(currentHourglass.Data, hNeighbor.Data,
                    "Replayed Hourglass: Chained into another Hourglass, repeating its entire sequence.");

                queue.Enqueue(hNeighbor);
            }

            if (queue.Count > 0)
            {
                // Snappy transition to chained hourglass
                yield return new WaitForSeconds(0.10f);
            }
        }

        yield return new WaitForSeconds(0.15f);
    }
}