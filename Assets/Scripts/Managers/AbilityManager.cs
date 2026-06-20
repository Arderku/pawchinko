using System.Collections.Generic;
using UnityEngine;

namespace Pawchinko
{
    /// <summary>
    /// Owns ability selection + resolution for a battle (Section 13). During the plan window the
    /// player picks at most one ability per active Pom; each pick is validated against the Pom's
    /// required type and current Action Points, then every locked ability is aggregated into a
    /// per-side <see cref="RoundModifiers"/> bag. Gameplay systems pull those modifiers when they
    /// act (ball count, spawn bias, per-ball power, bucket rules, energy %) so an ability is
    /// active for exactly the one round it was cast in.
    ///
    /// AP refills to <see cref="PomInstance.maxAP"/> at the start of every round; selections + the
    /// resolved modifiers are cleared at the same time. Player-only casting for now - the per-side
    /// split already supports enemy AI casting later.
    /// </summary>
    public class AbilityManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EventSystem eventSystem;

        private readonly RoundModifiers _playerMods = new();
        private readonly RoundModifiers _enemyMods = new();

        // One locked selection per active player Pom slot (index 0..MaxActivePoms-1). Null = none.
        private readonly PomAbilityData[] _playerSelection = new PomAbilityData[BattleManager.MaxActivePoms];

        public void Initialize(EventSystem eventSystem)
        {
            this.eventSystem = eventSystem;
            this.eventSystem.Subscribe<RoundStartedEvent>(OnRoundStarted);

            ClearSelectionAndMods(publish: false);

            Debug.Log("[AbilityManager] Initialized");
        }

        /// <summary>Resolved modifiers for the given side this round. Never null.</summary>
        public RoundModifiers GetModifiers(Side side)
        {
            return side == Side.Player ? _playerMods : _enemyMods;
        }

        /// <summary>The ability locked into the given active player slot, or null.</summary>
        public PomAbilityData GetSelection(int activeIndex)
        {
            if (activeIndex < 0 || activeIndex >= _playerSelection.Length) return null;
            return _playerSelection[activeIndex];
        }

        /// <summary>
        /// Locks (or clears, with <paramref name="ability"/> null) the player's ability choice for
        /// an active Pom slot. Validates the Pom's required type + AP, charging/refunding AP as the
        /// selection changes, then rebuilds the round modifiers and broadcasts the new state.
        /// Returns false (and changes nothing) if the pick is illegal or unaffordable.
        /// </summary>
        public bool SelectAbility(int activeIndex, PomAbilityData ability)
        {
            if (activeIndex < 0 || activeIndex >= _playerSelection.Length) return false;

            var bm = GameManager.Instance != null ? GameManager.Instance.BattleManager : null;
            if (bm == null)
            {
                Debug.LogError("[AbilityManager] BattleManager unavailable, cannot select ability.");
                return false;
            }
            var active = bm.GetActivePoms(Side.Player);
            if (active == null || activeIndex >= active.Count) return false;
            var pom = active[activeIndex];
            if (pom == null) return false;

            var previous = _playerSelection[activeIndex];

            if (ability != null)
            {
                if (!CanUse(pom, ability))
                {
                    Debug.LogWarning($"[AbilityManager] {SafeName(pom)} cannot use '{ability.DisplayName}' (required type mismatch).");
                    return false;
                }
                // AP available if we first refunded whatever was previously selected here.
                int available = pom.currentAP + (previous != null ? previous.ApCost : 0);
                if (ability.ApCost > available)
                {
                    Debug.Log($"[AbilityManager] {SafeName(pom)} cannot afford '{ability.DisplayName}' (cost {ability.ApCost}, available {available}).");
                    return false;
                }
            }

            // Commit: refund the previous pick, then charge the new one.
            if (previous != null) pom.currentAP = Mathf.Min(pom.maxAP, pom.currentAP + previous.ApCost);
            if (ability != null) pom.currentAP = Mathf.Max(0, pom.currentAP - ability.ApCost);
            _playerSelection[activeIndex] = ability;

            Rebuild();
            eventSystem?.Publish(new AbilityCastEvent(Side.Player, activeIndex, ability, pom.currentAP, pom.maxAP));
            return true;
        }

        private void OnRoundStarted(RoundStartedEvent evt)
        {
            RefillAllAP();
            ClearSelectionAndMods(publish: true);
        }

        private void RefillAllAP()
        {
            var bm = GameManager.Instance != null ? GameManager.Instance.BattleManager : null;
            if (bm == null) return;
            RefillRoster(bm.GetRoster(Side.Player));
            RefillRoster(bm.GetRoster(Side.Enemy));
        }

        private static void RefillRoster(IReadOnlyList<PomInstance> roster)
        {
            if (roster == null) return;
            for (int i = 0; i < roster.Count; i++)
            {
                var pom = roster[i];
                if (pom != null) pom.currentAP = pom.maxAP;
            }
        }

        private void ClearSelectionAndMods(bool publish)
        {
            for (int i = 0; i < _playerSelection.Length; i++) _playerSelection[i] = null;
            _playerMods.Clear();
            _enemyMods.Clear();
            if (publish) eventSystem?.Publish(new AbilitiesResolvedEvent(_playerMods, _enemyMods));
        }

        private void Rebuild()
        {
            _playerMods.Clear();
            _enemyMods.Clear();

            for (int i = 0; i < _playerSelection.Length; i++)
            {
                var ability = _playerSelection[i];
                if (ability != null) ApplyAbility(ability);
            }

            eventSystem?.Publish(new AbilitiesResolvedEvent(_playerMods, _enemyMods));
        }

        private void ApplyAbility(PomAbilityData ability)
        {
            var effects = ability.Effects;
            if (effects == null) return;
            for (int i = 0; i < effects.Count; i++)
            {
                var e = effects[i];
                if (e == null) continue;
                switch (ability.BoardTarget)
                {
                    case PomAbilityBoardTarget.Self:
                        ApplyEffect(_playerMods, e);
                        break;
                    case PomAbilityBoardTarget.Enemy:
                        ApplyEffect(_enemyMods, e);
                        break;
                    case PomAbilityBoardTarget.Both:
                        ApplyEffect(_playerMods, e);
                        ApplyEffect(_enemyMods, e);
                        break;
                }
            }
        }

        private static void ApplyEffect(RoundModifiers mods, AbilityEffect e)
        {
            switch (e.kind)
            {
                case AbilityEffectKind.BallPower:
                    // Per-ball chance is rolled at apply time, so keep the entry as-is.
                    mods.BallPower.Add(new RoundModifiers.BallPowerEntry
                    {
                        filter = e.typeFilter,
                        mode = e.mode,
                        amount = e.amount,
                        chance = e.chance
                    });
                    break;

                case AbilityEffectKind.BallCount:
                    if (!Roll(e.chance)) break;
                    if (e.mode == AbilityValueMode.Multiply) mods.BallCountMult *= e.amount;
                    else mods.BallCountAdd += Mathf.RoundToInt(e.amount); // Add/Set both act as a +/- delta
                    break;

                case AbilityEffectKind.BucketModifier:
                    if (e.targetIndices == null) break;
                    for (int i = 0; i < e.targetIndices.Length; i++)
                    {
                        if (!Roll(e.chance)) continue; // per-bucket roll
                        mods.Buckets.Add(new RoundModifiers.BucketEntry
                        {
                            slot = e.targetIndices[i],
                            filter = e.typeFilter,
                            typeExclusive = e.typeExclusive,
                            mode = e.mode,
                            amount = e.amount
                        });
                    }
                    break;

                case AbilityEffectKind.SpawnSlotBias:
                    mods.SpawnBias.Add(new RoundModifiers.SpawnBiasEntry
                    {
                        zones = e.targetIndices,
                        force = e.forceSpawn,
                        chance = e.chance
                    });
                    break;

                case AbilityEffectKind.EnergyPercent:
                    if (Roll(e.chance)) mods.EnergyPercent += e.amount;
                    break;

                case AbilityEffectKind.PegEffect:
                    if (e.pegAction == PegAction.Hide)
                    {
                        if (e.targetIndices != null)
                        {
                            for (int i = 0; i < e.targetIndices.Length; i++)
                            {
                                if (Roll(e.chance)) mods.HiddenPegs.Add(e.targetIndices[i]); // per-peg roll
                            }
                        }
                    }
                    else // PowerOnHit - per-hit chance is rolled when a ball hits the peg
                    {
                        mods.PegPower.Add(new RoundModifiers.PegPowerEntry
                        {
                            pegs = e.targetIndices,
                            filter = e.typeFilter,
                            mode = e.mode,
                            amount = e.amount,
                            chance = e.chance
                        });
                    }
                    break;
            }
        }

        private static bool CanUse(PomInstance pom, PomAbilityData ability)
        {
            if (pom == null || pom.data == null || ability == null) return false;
            var req = ability.RequiredType;
            if (req.any) return true;
            if (req.Matches(pom.data.PrimaryType)) return true;
            if (pom.data.HasSecondaryType && req.Matches(pom.data.SecondaryType)) return true;
            return false;
        }

        private static bool Roll(float chance)
        {
            return chance >= 1f || Random.value <= chance;
        }

        private static string SafeName(PomInstance pom)
        {
            return pom != null && pom.data != null ? pom.data.DisplayName : "<null>";
        }

        private void OnDestroy()
        {
            if (eventSystem == null) return;
            eventSystem.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
        }
    }
}
