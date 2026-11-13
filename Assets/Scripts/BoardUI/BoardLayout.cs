#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using UnityEngine;
using PlantsVsZombies.Game;

namespace PlantsVsZombies.BoardUI
{
    /// <summary>Converts board cells, seed packets, coins and shovel targets between the original coordinate space and screen rectangles.</summary>
    internal sealed class BoardLayout
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose active board the layout describes.</param>
        internal BoardLayout(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Stores grid x.</summary>
        internal const float GridX = 70f;
        /// <summary>Stores grid y.</summary>
        internal const float GridY = 80f;
        /// <summary>Stores cell width.</summary>
        internal const float CellWidth = 80f;
        /// <summary>Stores cell height.</summary>
        internal const float CellHeight = 100f;

        /// <summary>Performs seed rect.</summary>
        /// <param name = "index">The index.</param>
        /// <returns>The resulting seed rect.</returns>
        internal Rect SeedRect(int index)
        {
            if (_owner.RequiredGame.IsAdventure)
            {
                return AdventureSeedRect(index);
            }

            if (_owner.RequiredGame.IsExperimentalLevelOne)
            {
                var slot = FirstAdventureBoardUi.FirstSeedSlot(0, 0);
                return new Rect(slot.X + 59 * index, slot.Y, slot.Width, slot.Height);
            }

            return new Rect(AdventureSeedSlotLayout.PositionX(index, _owner._choosingSeeds ? 7 : _owner._deck.Count), 8, 50, 70);
        }

        /// <summary>Performs chooser seed rect.</summary>
        /// <param name = "index">The index.</param>
        /// <returns>The resulting chooser seed rect.</returns>
        internal static Rect ChooserSeedRect(int index)
        {
            return new Rect(198 + index % 6 * 65, 150 + index / 6 * 84, 50, 70);
        }

        /// <summary>Performs sun rect.</summary>
        /// <param name = "sun">The sun.</param>
        /// <returns>The resulting sun rect.</returns>
        internal static Rect SunRect(global::PlantsVsZombies.Simulation.Sun sun)
        {
            return new Rect(GridX + sun.X * CellWidth - 35, GridY + sun.Y * CellHeight - 35, 70, 70);
        }

        /// <summary>Stores the PC conveyor clipping rectangle relative to the seed bank.</summary>
        private static readonly Rect s_nativeConveyorClip = new Rect(90, 0, 501, 87);

        /// <summary>Gets the PC conveyor's bank origin through introduction and gameplay.</summary>
        /// <remarks>PC 0x425CC4 adds the bank position; 0x44778C retains X=10 after the introduction.</remarks>
        internal Vector2 AdventureConveyorOrigin => !_owner.IndependentModes.IsModeBoard && _owner.Opening._adventureIntroFrame == null
            ? new Vector2(10f, 0f) : AdventureSeedBankOrigin;

        /// <summary>Gets the conveyor clipping rectangle at the same origin used by its artwork and input.</summary>
        internal Rect AdventureConveyorClip
        {
            get
            {
                var clip = s_nativeConveyorClip;
                clip.position += AdventureConveyorOrigin;
                return clip;
            }
        }

        /// <summary>Performs adventure packet contains.</summary>
        /// <param name = "index">The index.</param>
        /// <param name = "point">The point.</param>
        /// <returns>The resulting adventure packet contains.</returns>
        internal bool AdventurePacketContains(int index, Vector2 point)
        {
            return (!_owner.RequiredGame.UsesNativeAdventureConveyor || AdventureConveyorClip.Contains(point)) && AdventureSeedRect(index, true).Contains(point);
        }

        /// <summary>Stores adventure grid x.</summary>
        internal const float AdventureGridX = 40f;
        /// <summary>Stores adventure grid y.</summary>
        internal const float AdventureGridY = 80f;
        /// <summary>Gets or sets adventure pitch.</summary>
        internal float AdventurePitch
        {
            get
            {
                return _owner.RequiredGame.Adventure.Level.HasPool || _owner.RequiredGame.Adventure.Level.HasRoof ? 85f : 100f;
            }
        }

        /// <summary>Performs adventure slope.</summary>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting adventure slope.</returns>
        internal float AdventureSlope(float column)
        {
            return _owner.RequiredGame.Adventure.Level.HasRoof ? Mathf.Max(0f, 5f - column) * 20f - 10f : 0f;
        }

        /// <summary>Performs adventure cell y.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting adventure cell y.</returns>
        internal float AdventureCellY(int row, float column)
        {
            return AdventureGridY + row * AdventurePitch + AdventureSlope(column);
        }

        /// <summary>Performs adventure coin rect.</summary>
        /// <param name = "coin">The coin.</param>
        /// <returns>The resulting adventure coin rect.</returns>
        internal Rect AdventureCoinRect(AdventureCoin coin)
        {
            return coin.IsBeingCollected ? new Rect(0f, 0f, 0f, 0f) : new Rect(coin.PresentationX, coin.PresentationY, 40f, 40f);
        }

        /// <summary>Performs adventure sun rect.</summary>
        /// <param name = "sun">The sun.</param>
        /// <returns>The resulting adventure sun rect.</returns>
        internal Rect AdventureSunRect(global::PlantsVsZombies.Simulation.Sun sun)
        {
            if (sun.PresentationNative)
            {
                var pickup = sun.PresentationPickup;
                if (pickup.Dead || pickup.Collecting)
                {
                    return new Rect(0, 0, 0, 0);
                }

                var margin = pickup.Kind == global::PlantsVsZombies.Sun.SunKind.Normal || sun.PresentationSpecialExpansion ? 15f : 0f;
                return new Rect(pickup.X - margin, pickup.Y - margin, 60 + margin * 2, 60 + margin * 2 + (sun.PresentationSpecialExpansion ? 30 : 0));
            }

            var fallbackMargin = sun.Value == 25 ? 15f : 0f;
            return new Rect(AdventureGridX + sun.X * CellWidth - 30f - fallbackMargin, AdventureGridY + sun.Y * AdventurePitch - 30f - fallbackMargin, 60 + fallbackMargin * 2, 60 + fallbackMargin * 2);
        }

        /// <summary>Finds the exact plant highlighted and removed by the held shovel.</summary>
        /// <param name="point">The logical pointer position on the board.</param>
        /// <returns>The native tool target, or null when no removable plant is under the pointer.</returns>
        internal Plant? AdventureShovelTarget(Vector2 point)
        {
            if (!_owner.RequiredGame.IsAdventure || point.y < 80f || point.x < 0f || point.x >= 800f || point.y >= 600f)
            {
                return null;
            }

            var column = Mathf.FloorToInt((point.x - AdventureGridX) / CellWidth);
            var row = Mathf.FloorToInt((point.y - AdventureGridY - AdventureSlope(column)) / AdventurePitch);
            Plant? main = null, support = null, pumpkin = null;
            foreach (var plant in _owner.RequiredGame.Plants)
            {
                if (plant.Kind == PlantKind.Squash && plant.PresentationBiteNotOnGround)
                {
                    continue;
                }

                var x = AdventureGridX + plant.Column * CellWidth;
                var y = AdventureCellY(plant.Row, plant.Column);
                if (plant.Kind == PlantKind.Pumpkin)
                {
                    var hasMain = false;
                    foreach (var other in _owner.RequiredGame.Plants)
                    {
                        if (other.Row == plant.Row && other.Layer == PlantLayer.Main && other.Column <= plant.Column && plant.Column < other.Column + other.ColumnSpan
                            && !(other.Kind == PlantKind.Squash && other.PresentationBiteNotOnGround))
                        {
                            hasMain = true;
                            break;
                        }
                    }

                    var distance = (point - new Vector2(x + 40f, y + 40f)).sqrMagnitude;
                    if (distance >= (hasMain ? 625f : 0f) && distance <= 2500f && point.y > y + 25f)
                    {
                        return plant;
                    }
                }

                if (plant.Kind == PlantKind.CoffeeBean && (point - new Vector2(x + 40f, y)).sqrMagnitude < 225f)
                {
                    return plant;
                }

                if (plant.Row != row || plant.Column > column || column >= plant.Column + plant.ColumnSpan)
                {
                    continue;
                }

                if (plant.Layer == PlantLayer.Main)
                {
                    main = plant;
                }
                else if (plant.Layer == PlantLayer.Support)
                {
                    support = plant;
                }
                else if (plant.Kind == PlantKind.Pumpkin)
                {
                    pumpkin = plant;
                }
            }

            var target = main ?? (pumpkin == null ? support : null);
            return target?.Kind == PlantKind.GraveBuster ? null : target;
        }

        /// <summary>Performs adventure seed rect.</summary>
        /// <param name = "index">The index.</param>
        /// <param name = "packet">The packet.</param>
        /// <returns>The resulting adventure seed rect.</returns>
        internal Rect AdventureSeedRect(int index, bool packet = false)
        {
            if (packet && _owner._game != null && _owner._game.UsesNativeAdventureConveyor && index < _owner._game.Adventure.Packets.Count)
            {
                var conveyor = AdventureConveyorOrigin;
                return new Rect(conveyor.x + _owner._game.AdventureConveyorCardX(index), conveyor.y + 8f, 50f, 70f);
            }

            if (packet)
            {
                return new Rect(8f + index * 50f, 8f, 50f, 70f);
            }

            var origin = AdventureSeedBankOrigin;
            return new Rect(origin.x + AdventureSeedSlotLayout.PositionX(index, SeedBankCapacity), origin.y + 8f, 50f, 70f);
        }

        /// <summary>Gets the bank's configured capacity independently of how many cards are currently selected.</summary>
        /// <remarks>Early first-adventure levels retain their original fixed packet counts.</remarks>
        internal int SeedBankCapacity
        {
            get
            {
                if (_owner.IndependentModes.IsModeBoard)
                {
                    return _owner.IndependentModes.RequiredModeSession.PacketSlots;
                }

                if (_owner.RequiredGame.IsAdventure)
                {
                    return _owner.RequiredGame.Adventure.Level.PacketSlots;
                }

                return _owner._choosingSeeds ? 7 : _owner._deck.Count;
            }
        }

        /// <summary>Gets the original shovel holder and click rectangle beside the current bank.</summary>
        /// <remarks>PC 0x416BC0 uses bank capacity; 0x427AF0 fixes conveyor capacity at ten.</remarks>
        internal Rect AdventureShovelButton
        {
            get
            {
                if (_owner.IndependentModes.IsModeBoard && _owner.IndependentModes.RequiredModeSession.Definition.Id == 18)
                {
                    return new Rect(600f, 0f, 70f, 72f);
                }

                var capacity = _owner.RequiredGame.UsesNativeAdventureConveyor ? 10 : SeedBankCapacity;
                return new Rect(456f + AdventureSeedSlotLayout.BackgroundExtra(capacity), 0f, 70f, 72f);
            }
        }

        /// <summary>Gets the native bank position, retaining the final introduction pose during the playing handoff.</summary>
        internal Vector2 AdventureSeedBankOrigin
        {
            get
            {
                if (!_owner.IndependentModes.IsModeBoard && _owner.RequiredGame.IsAdventure && _owner.RequiredGame.Adventure.Level.Mode == AdventureMode.WhackAZombie)
                {
                    var x = _owner.Opening._adventureIntroFrame == null ? 0f : _owner.Opening._adventureIntroFrame.SeedBankX;
                    return new Vector2(x, _owner.RequiredGame.Adventure._whackSeedBankY);
                }

                return !_owner.IndependentModes.IsModeBoard && _owner.RequiredGame.IsAdventure && _owner.Opening._adventureIntroFrame != null
                    ? new Vector2(_owner.Opening._adventureIntroFrame.SeedBankX, _owner.Opening._adventureIntroFrame.SeedBankY)
                    : Vector2.zero;
            }
        }

        /// <summary>Performs adventure chooser rect.</summary>
        /// <param name = "index">The index.</param>
        /// <returns>The resulting adventure chooser rect.</returns>
        internal static Rect AdventureChooserRect(int index)
        {
            return new Rect(118f + index % 10 * 57f, 126f + index / 10 * 80f, 50f, 70f);
        }
    }
}
