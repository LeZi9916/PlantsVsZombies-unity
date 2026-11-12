#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Preserves seed selection order and the original PC chooser packet flights.</summary>
    public sealed class AdventureSeedChooserState : IReadOnlyList<PlantKind>
    {
        /// <summary>Stores selected packets in their bank order.</summary>
        private readonly List<PlantKind> _seeds = new List<PlantKind>(10);
        /// <summary>Stores the last motion configured for each packet.</summary>
        private readonly Dictionary<PlantKind, Motion> _motions = new Dictionary<PlantKind, Motion>();
        /// <summary>Stores the chooser age in native hundredths of a second.</summary>
        private int _age;
        /// <summary>Stores the last completion tick among current packet flights.</summary>
        private int _motionEnd;
        /// <summary>Retains sub-tick elapsed time between presentation updates.</summary>
        private double _remainder;

        /// <summary>Initializes packets that are already settled in their selected slots.</summary>
        /// <param name="seeds">The initial packets in bank order.</param>
        /// <exception cref="ArgumentNullException">The initial selection is null.</exception>
        public AdventureSeedChooserState(IEnumerable<PlantKind> seeds)
        {
            if (seeds == null)
            {
                throw new ArgumentNullException(nameof(seeds));
            }

            foreach (var seed in seeds)
            {
                Add(seed);
            }
        }

        /// <summary>Gets the number of selected seed packets.</summary>
        public int Count => _seeds.Count;
        /// <summary>Gets whether a packet is still travelling between chooser or bank positions.</summary>
        public bool HasFlyingSeeds => _age < _motionEnd;
        /// <summary>Gets the packet at the given bank position.</summary>
        /// <param name="index">The zero-based bank slot.</param>
        /// <returns>The selected packet in that slot.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The slot is outside the selected range.</exception>
        public PlantKind this[int index] => _seeds[index];

        /// <summary>Checks whether a packet belongs to the selected bank.</summary>
        /// <param name="kind">The seed packet to check.</param>
        /// <returns>Whether the packet is selected.</returns>
        public bool Contains(PlantKind kind)
        {
            return _seeds.Contains(kind);
        }

        /// <summary>Appends an initial or restored packet without duplicating a selected kind.</summary>
        /// <param name="kind">The packet to append.</param>
        public void Add(PlantKind kind)
        {
            if (!Contains(kind))
            {
                _seeds.Add(kind);
            }
        }

        /// <summary>Removes a packet immediately when restoring a nonanimated selection.</summary>
        /// <param name="kind">The packet to remove.</param>
        /// <returns>Whether a selected packet was removed.</returns>
        public bool Remove(PlantKind kind)
        {
            return _seeds.Remove(kind);
        }

        /// <summary>Starts the original 25-tick flight into the next vacant bank slot.</summary>
        /// <param name="kind">The unselected packet to move.</param>
        /// <param name="homeX">The packet's chooser X coordinate.</param>
        /// <param name="homeY">The packet's chooser Y coordinate.</param>
        /// <param name="slots">The bank's configured capacity.</param>
        /// <returns>Whether a packet flight started.</returns>
        public bool Choose(PlantKind kind, int homeX, int homeY, int slots)
        {
            if (HasFlyingSeeds || Contains(kind) || Count >= slots)
            {
                return false;
            }

            StartMotion(kind, homeX, homeY, AdventureSeedSlotLayout.PositionX(Count, slots), 8, 25);
            Add(kind);
            return true;
        }

        /// <summary>Returns a packet in 25 ticks and closes subsequent bank gaps in 15 ticks.</summary>
        /// <param name="kind">The selected packet to return.</param>
        /// <param name="homeX">The packet's chooser X coordinate.</param>
        /// <param name="homeY">The packet's chooser Y coordinate.</param>
        /// <param name="slots">The bank's configured capacity.</param>
        /// <returns>Whether a selected packet began its return flight.</returns>
        public bool Return(PlantKind kind, int homeX, int homeY, int slots)
        {
            var index = _seeds.IndexOf(kind);
            if (HasFlyingSeeds || index < 0)
            {
                return false;
            }

            StartMotion(kind, AdventureSeedSlotLayout.PositionX(index, slots), 8, homeX, homeY, 25);
            for (var i = index + 1; i < Count; i++)
            {
                StartMotion(_seeds[i], AdventureSeedSlotLayout.PositionX(i, slots), 8,
                    AdventureSeedSlotLayout.PositionX(i - 1, slots), 8, 15);
            }

            _seeds.RemoveAt(index);
            return true;
        }

        /// <summary>Completes current flights before the next click, matching the original chooser mouse handling.</summary>
        public void LandFlyingSeeds()
        {
            _age = Math.Max(_age, _motionEnd);
            _remainder = 0;
        }

        /// <summary>Advances packet flights at the original 100 Hz update rate.</summary>
        /// <param name="seconds">The elapsed active chooser time in seconds.</param>
        public void Advance(float seconds)
        {
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds))
            {
                return;
            }

            _remainder += seconds;
            var ticks = (int)((_remainder + 0.00000001) / 0.01);
            _remainder -= ticks * 0.01;
            _age += ticks;
        }

        /// <summary>Checks whether a particular packet is in flight.</summary>
        /// <param name="kind">The packet to inspect.</param>
        /// <returns>Whether the packet's motion has not reached its final tick.</returns>
        public bool IsMoving(PlantKind kind)
        {
            return _motions.TryGetValue(kind, out var motion) && _age < motion.End;
        }

        /// <summary>Samples a packet in logical screen coordinates with the original curve 4.</summary>
        /// <param name="kind">The packet to position.</param>
        /// <param name="homeX">Its resting chooser X coordinate.</param>
        /// <param name="homeY">Its resting chooser Y coordinate.</param>
        /// <param name="slots">The bank's configured capacity.</param>
        /// <param name="x">Receives the current logical X coordinate.</param>
        /// <param name="y">Receives the current logical Y coordinate.</param>
        public void GetPosition(PlantKind kind, int homeX, int homeY, int slots, out int x, out int y)
        {
            if (_motions.TryGetValue(kind, out var motion) && _age < motion.End)
            {
                x = AdventureIntroPresentation.Curve(motion.Start, motion.End, _age, motion.FromX, motion.ToX, 4);
                y = AdventureIntroPresentation.Curve(motion.Start, motion.End, _age, motion.FromY, motion.ToY, 4);
                return;
            }

            var index = _seeds.IndexOf(kind);
            x = index >= 0 ? AdventureSeedSlotLayout.PositionX(index, slots) : homeX;
            y = index >= 0 ? 8 : homeY;
        }

        /// <summary>Enumerates selected packets in their exact bank order.</summary>
        /// <returns>A reusable value-type list enumerator.</returns>
        public List<PlantKind>.Enumerator GetEnumerator()
        {
            return _seeds.GetEnumerator();
        }

        /// <summary>Enumerates packets through the generic collection contract.</summary>
        /// <returns>An enumerator over the selected bank order.</returns>
        IEnumerator<PlantKind> IEnumerable<PlantKind>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>Enumerates packets through the nongeneric collection contract.</summary>
        /// <returns>An enumerator over the selected bank order.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>Configures a native packet motion without allocating an animation object.</summary>
        /// <param name="kind">The moving packet.</param>
        /// <param name="fromX">The initial X coordinate.</param>
        /// <param name="fromY">The initial Y coordinate.</param>
        /// <param name="toX">The final X coordinate.</param>
        /// <param name="toY">The final Y coordinate.</param>
        /// <param name="ticks">The motion's native duration.</param>
        private void StartMotion(PlantKind kind, int fromX, int fromY, int toX, int toY, int ticks)
        {
            // PC SeedChooserScreen::ClickedSeedInChooser 0x49BB30 / ClickedSeedInBank 0x49B990.
            _motions[kind] = new Motion
            {
                Start = _age,
                End = _age + ticks,
                FromX = fromX,
                FromY = fromY,
                ToX = toX,
                ToY = toY
            };
            _motionEnd = Math.Max(_motionEnd, _age + ticks);
        }

        /// <summary>Stores the original integer endpoints and hundredth-second motion bounds.</summary>
        private struct Motion
        {
            /// <summary>Stores the first motion tick.</summary>
            public int Start;
            /// <summary>Stores the completion tick.</summary>
            public int End;
            /// <summary>Stores the starting X coordinate.</summary>
            public int FromX;
            /// <summary>Stores the starting Y coordinate.</summary>
            public int FromY;
            /// <summary>Stores the target X coordinate.</summary>
            public int ToX;
            /// <summary>Stores the target Y coordinate.</summary>
            public int ToY;
        }
    }
}
