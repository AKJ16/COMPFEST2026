using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeaponEffectsSystem : MonoBehaviour
{
    public static WeaponEffectsSystem Instance { get; private set; }

    /// <summary>
    /// True while any weapon attack, particle effect,
    /// or Hourglass chain is running.
    /// </summary>
    public static bool IsBusy { get; private set; } = false;

    [SerializeField] private EnemyHealth currentEnemy;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    // =========================================================
    // STAGE
    // =========================================================

    public void StartStage(EnemyHealth enemy)
    {
        currentEnemy = enemy;
        IsBusy = false;
    }


    // =========================================================
    // PLACEMENT
    // =========================================================

    public void ResolvePlacement(WeaponInstance instance)
    {
        if (instance == null || instance.Data == null)
            return;

        StartCoroutine(
            ResolvePlacementRoutine(instance)
        );
    }


    private IEnumerator ResolvePlacementRoutine(
        WeaponInstance instance)
    {
        IsBusy = true;


        // =====================================================
        // 1. TICK EXISTING POISON
        // =====================================================

        if (currentEnemy != null)
        {
            currentEnemy.TickPoisonTurn();
        }


        // =====================================================
        // 2. RESOLVE WEAPON
        // =====================================================

        switch (instance.Data.category)
        {
            // -------------------------------------------------
            // ATTACK
            // -------------------------------------------------

            case WeaponCategory.Attack:

                ResolveAttack(instance);

                yield return new WaitForSeconds(0.35f);

                break;


            // -------------------------------------------------
            // MODIFIER
            // -------------------------------------------------

            case WeaponCategory.Modifier:

                if (instance.Data.modifierType ==
                    ModifierType.Repeat)
                {
                    if (VFXManager.Instance != null)
                    {
                        VFXManager.Instance.BeginHourglassRewind();
                    }

                    yield return StartCoroutine(
                        HourglassChainRoutine(instance)
                    );

                    if (VFXManager.Instance != null)
                    {
                        VFXManager.Instance.EndHourglassRewind();
                    }
                }
                else
                {
                    PlayBookScreenFeedback(
                        instance.Data
                    );

                    yield return new WaitForSeconds(
                        0.30f
                    );
                }

                break;


            // -------------------------------------------------
            // UTILITY
            // -------------------------------------------------

            case WeaponCategory.Utility:

                PlayAttackFeedback(
                    instance.Data
                );

                yield return new WaitForSeconds(
                    0.20f
                );

                break;
        }


        // =====================================================
        // 3. SMALL BUFFER
        // =====================================================

        yield return new WaitForSeconds(0.15f);


        // =====================================================
        // 4. UNLOCK
        // =====================================================

        IsBusy = false;


        // =====================================================
        // 5. CHECK STAGE
        // =====================================================

        if (StageManager.Instance != null)
        {
            StageManager.Instance.CheckForEndOfStage();
        }
    }


    // =========================================================
    // ATTACK RESOLUTION
    // =========================================================

    private void ResolveAttack(
        WeaponInstance attackInstance)
    {
        if (attackInstance == null ||
            attackInstance.Data == null)
        {
            return;
        }


        float multiplier = 1f;
        float addition = 0f;


        // -----------------------------------------------------
        // FIND ADJACENT BOOKS
        // -----------------------------------------------------

        var adjacentModifiers =
            WeaponGridManager.Instance
                .GetNeighborsOf(attackInstance)
                .Where(
                    n =>
                        n != null &&
                        n.Data != null &&
                        n.Data.category ==
                            WeaponCategory.Modifier &&
                        n.Data.modifierType !=
                            ModifierType.Repeat
                );


        var boostingBooks =
            new List<WeaponInstance>();


        // -----------------------------------------------------
        // APPLY BOOK EFFECTS
        // -----------------------------------------------------

        foreach (var modifier in adjacentModifiers)
        {
            if (modifier.Data.targets == null)
                continue;


            if (!modifier.Data.targets.Contains(
                attackInstance.Data))
            {
                continue;
            }


            switch (modifier.Data.modifierType)
            {
                case ModifierType.Multiplier:

                    multiplier *=
                        modifier.Data.modifierValue;

                    if (!Mathf.Approximately(
                        modifier.Data.modifierValue,
                        1f))
                    {
                        boostingBooks.Add(
                            modifier
                        );
                    }

                    break;


                case ModifierType.Addition:

                    addition +=
                        modifier.Data.modifierValue;

                    if (!Mathf.Approximately(
                        modifier.Data.modifierValue,
                        0f))
                    {
                        boostingBooks.Add(
                            modifier
                        );
                    }

                    break;
            }
        }


        // -----------------------------------------------------
        // FINAL DAMAGE
        // -----------------------------------------------------

        int finalDamage =
            Mathf.RoundToInt(
                (attackInstance.Data.baseDamage *
                 multiplier) +
                addition
            );


        // IMPORTANT:
        // Save damage snapshot for Hourglass.
        attackInstance.ResolvedDamage =
            finalDamage;


        // -----------------------------------------------------
        // DAMAGE ENEMY
        // -----------------------------------------------------

        if (currentEnemy != null)
        {
            currentEnemy.TakeDamage(
                finalDamage
            );


            // Poison
            if (attackInstance.Data.appliesPoison)
            {
                currentEnemy.ApplyPoison(
                    attackInstance,
                    attackInstance.Data.poisonDamagePerTick
                );
            }


            // -------------------------------------------------
            // HANDBOOK
            // -------------------------------------------------

            foreach (var book in boostingBooks)
            {
                HandbookUI.ReportCombo(
                    book.Data,
                    attackInstance.Data,
                    BuildBookComboDetail(
                        book.Data,
                        attackInstance.Data,
                        boostingBooks.Count,
                        finalDamage
                    )
                );
            }
        }


        // -----------------------------------------------------
        // VFX + SFX
        // -----------------------------------------------------

        PlayAttackFeedback(
            attackInstance.Data
        );
    }


    // =========================================================
    // BOOK COMBO DETAIL
    // =========================================================

    private static string BuildBookComboDetail(
        WeaponData book,
        WeaponData target,
        int bookCount,
        int finalDamage)
    {
        int baseDamage =
            target.baseDamage;

        float value =
            book.modifierValue;


        string detail;


        if (book.modifierType ==
            ModifierType.Multiplier)
        {
            detail =
                $"{target.weaponName} damage went from " +
                $"{baseDamage} to " +
                $"{Mathf.RoundToInt(baseDamage * value)} " +
                $"(x{value:0.##})";
        }
        else
        {
            detail =
                $"{target.weaponName} damage went from " +
                $"{baseDamage} to " +
                $"{Mathf.RoundToInt(baseDamage + value)} " +
                $"(+{value:0.##})";
        }


        if (bookCount > 1)
        {
            detail +=
                $". With every book in range the hit was " +
                $"{finalDamage}";
        }


        return detail + ".";
    }


    // =========================================================
    // BOOK SCREEN FEEDBACK
    // =========================================================

    private void PlayBookScreenFeedback(
        WeaponData data)
    {
        if (data == null ||
            VFXManager.Instance == null)
        {
            return;
        }


        if (data.modifierType ==
            ModifierType.Addition)
        {
            VFXManager.Instance
                .PlayAdditionScreenEffect();
        }
        else if (data.modifierType ==
                 ModifierType.Multiplier)
        {
            VFXManager.Instance
                .PlayMultiplicationScreenEffect();
        }
        else if (data.modifierType ==
                 ModifierType.Repeat)
        {
            VFXManager.Instance
                .PlayHourglassScreenEffect();
        }
    }


    // =========================================================
    // ATTACK VFX / SFX
    // =========================================================

    private void PlayAttackFeedback(
        WeaponData data)
    {
        if (data == null)
            return;


        Vector3 feedbackPosition =
            currentEnemy != null
                ? currentEnemy.transform.position
                : transform.position;


        feedbackPosition.z = 0f;


        // -----------------------------------------------------
        // SOUND
        // -----------------------------------------------------

        if (data.attackSfx != null &&
            AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(
                data.attackSfx
            );
        }


        // -----------------------------------------------------
        // WEAPON NAME
        // -----------------------------------------------------

        string weaponName = "";


        if (!string.IsNullOrEmpty(
            data.weaponName))
        {
            weaponName =
                data.weaponName.ToLower();
        }
        else if (data.name != null)
        {
            weaponName =
                data.name.ToLower();
        }


        Debug.Log(
            "[WeaponEffectsSystem] Attack Feedback: " +
            weaponName
        );


        // =====================================================
        // SWORD
        // =====================================================

        if (weaponName.Contains("sword") ||
            weaponName.Contains("pedang") ||
            weaponName.Contains("blade"))
        {
            Debug.Log(
                "[WeaponEffectsSystem] >>> SWORD VFX"
            );

            WeaponVfxSword.Play(
                feedbackPosition
            );

            return;
        }


        // =====================================================
        // POISON DAGGER
        // =====================================================

        if ((weaponName.Contains("poison") &&
             weaponName.Contains("dagger")) ||
            weaponName.Contains("poisondagger") ||
            weaponName.Contains("poison_dagger") ||
            weaponName.Contains("belati") ||
            weaponName.Contains("dagger") ||
            weaponName.Contains("knife"))
        {
            Debug.Log(
                "[WeaponEffectsSystem] >>> POISON DAGGER VFX"
            );

            WeaponVfxPoison.Play(
                feedbackPosition
            );

            return;
        }


        // =====================================================
        // STAFF
        // =====================================================

        if (weaponName.Contains("staff") ||
            weaponName.Contains("tongkat") ||
            weaponName.Contains("wand"))
        {
            Debug.Log(
                "[WeaponEffectsSystem] >>> STAFF VFX"
            );

            WeaponVfxStaff.Play(
                feedbackPosition
            );

            return;
        }


        // =====================================================
        // HOURGLASS
        // =====================================================

        if (weaponName.Contains("hourglass") ||
            weaponName.Contains("hour glass") ||
            weaponName.Contains("glass") ||
            weaponName.Contains("jam pasir") ||
            data.modifierType ==
                ModifierType.Repeat)
        {
            Debug.Log(
                "[WeaponEffectsSystem] >>> HOURGLASS VFX"
            );

            WeaponVfxHourglass.Play(
                feedbackPosition
            );

            return;
        }


        // =====================================================
        // ADDITION BOOK
        // =====================================================

        if (weaponName.Contains("addition") ||
            weaponName.Contains("book of addition") ||
            weaponName.Contains("add") ||
            weaponName.Contains("penambahan"))
        {
            Debug.Log(
                "[WeaponEffectsSystem] >>> ADDITION BOOK VFX"
            );

            WeaponVfxBooks.PlayAddition(
                feedbackPosition
            );

            return;
        }


        // =====================================================
        // MULTIPLIER BOOK
        // =====================================================

        if (weaponName.Contains("multiplier") ||
            weaponName.Contains("multiplication") ||
            weaponName.Contains("book of multiplier") ||
            weaponName.Contains("multiply") ||
            weaponName.Contains("perkalian"))
        {
            Debug.Log(
                "[WeaponEffectsSystem] >>> MULTIPLIER BOOK VFX"
            );

            WeaponVfxBooks.PlayMultiplier(
                feedbackPosition
            );

            return;
        }


        // =====================================================
        // FALLBACK
        // =====================================================

        Debug.Log(
            "[WeaponEffectsSystem] >>> GENERIC VFX"
        );


        if (WeaponVfx.Instance != null)
        {
            WeaponVfx.Instance.Play(
                data,
                feedbackPosition
            );
        }
    }


    // =========================================================
    // HOURGLASS CHAIN
    // =========================================================

    private IEnumerator HourglassChainRoutine(
        WeaponInstance initialHourglass)
    {
        var visitedHourglasses =
            new HashSet<WeaponInstance>();

        var queue =
            new Queue<WeaponInstance>();


        visitedHourglasses.Add(
            initialHourglass
        );

        queue.Enqueue(
            initialHourglass
        );


        while (queue.Count > 0)
        {
            var currentHourglass =
                queue.Dequeue();


            // -------------------------------------------------
            // HOURGLASS VFX
            // -------------------------------------------------

            PlayAttackFeedback(
                currentHourglass.Data
            );


            if (currentHourglass.VisualObject != null)
            {
                currentHourglass.VisualObject
                    .PlayAnimation();
            }


            yield return new WaitForSeconds(
                0.70f
            );


            // -------------------------------------------------
            // FIND NEIGHBORS
            // -------------------------------------------------

            var allNeighbors =
                WeaponGridManager.Instance
                    .GetNeighborsOf(
                        currentHourglass
                    )
                    .ToList();


            // -------------------------------------------------
            // REPLAY ATTACK WEAPONS
            // -------------------------------------------------

            var attackNeighbors =
                allNeighbors.Where(
                    n =>
                        n != null &&
                        n.Data != null &&
                        n.Data.category ==
                            WeaponCategory.Attack
                );


            foreach (var neighbor in attackNeighbors)
            {
                // IMPORTANT:
                // Use saved damage.
                // Do NOT recalculate buffs.

                int replayDamage =
                    neighbor.ResolvedDamage;


                if (currentEnemy != null)
                {
                    currentEnemy.TakeDamage(
                        replayDamage
                    );


                    string detail =
                        $"Replayed {neighbor.Data.weaponName}: " +
                        $"{replayDamage} damage dealt again";


                    if (neighbor.Data.appliesPoison)
                    {
                        currentEnemy.ApplyPoison(
                            neighbor,
                            neighbor.Data.poisonDamagePerTick
                        );

                        detail +=
                            ", and its poison was applied again";
                    }


                    HandbookUI.ReportCombo(
                        currentHourglass.Data,
                        neighbor.Data,
                        detail + "."
                    );
                }


                // -------------------------------------------------
                // REPLAY VFX
                // -------------------------------------------------

                PlayAttackFeedback(
                    neighbor.Data
                );


                if (neighbor.VisualObject != null)
                {
                    neighbor.VisualObject
                        .PlayAnimation();
                }


                yield return new WaitForSeconds(
                    0.80f
                );


                // -------------------------------------------------
                // STOP IF ENEMY DEAD
                // -------------------------------------------------

                if (currentEnemy != null &&
                    currentEnemy.State ==
                        EnemyState.Dead)
                {
                    yield break;
                }
            }


            // -------------------------------------------------
            // QUEUE OTHER HOURGLASSES
            // -------------------------------------------------

            var adjacentHourglasses =
                allNeighbors.Where(
                    n =>
                        n != null &&
                        n.Data != null &&
                        n.Data.modifierType ==
                            ModifierType.Repeat
                );


            foreach (var hNeighbor in adjacentHourglasses)
            {
                if (!visitedHourglasses.Contains(
                    hNeighbor))
                {
                    visitedHourglasses.Add(
                        hNeighbor
                    );

                    queue.Enqueue(
                        hNeighbor
                    );
                }
            }


            if (queue.Count > 0)
            {
                yield return new WaitForSeconds(
                    0.40f
                );
            }
        }


        yield return new WaitForSeconds(
            0.30f
        );
    }
}