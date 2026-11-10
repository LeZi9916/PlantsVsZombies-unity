#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure burn presentation rules.</summary>
    public static class AdventureBurnPresentationRules
    {
        // Exact branch order and constants from the PC function 0x54E920..0x54EDB3.
        /// <summary>Performs resolve.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "phase">The phase.</param>
        /// <param name = "inPool">The in pool.</param>
        /// <param name = "flying">The flying.</param>
        /// <param name = "hasHead">The has head.</param>
        /// <param name = "hasSled">The has sled.</param>
        /// <param name = "backwards">The backwards.</param>
        /// <param name = "hasObject">The has object.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "healthBeforeHit">The health before hit.</param>
        /// <returns>The resulting resolve.</returns>
        public static AdventureBurnPose Resolve(int kind, int phase, bool inPool, bool flying, bool hasHead, bool hasSled, bool backwards, bool hasObject, float scale, float healthBeforeHit)
        {
            var result = new AdventureBurnPose();
            if (kind == 25 || healthBeforeHit >= 1800f)
            {
                result.Visual = AdventureBurnVisual.NormalDeath;
                return result;
            }

            if (inPool || phase == 1 || phase == 3 || phase == 12 || phase == 14 || phase == 32 || phase == 33 || phase == 34 || phase == 35 || phase == 50 || phase == 52 || phase == 53 || phase == 54 || phase == 58 || phase == 71)
            {
                return result;
            }

            if (kind == 19 || kind == 20 || kind >= 26 && kind <= 31 || hasSled || flying || !hasHead)
            {
                result.Visual = AdventureBurnVisual.FrozenOriginal;
                result.Scale = scale;
                result.FacingLeft = backwards;
                return result;
            }

            result.Visual = AdventureBurnVisual.CharredDefinition;
            result.X = 22f;
            result.Y = -10f;
            result.ReanimationId = 36;
            switch (kind)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                case 13:
                case 14:
                case 15:
                case 16:
                case 18:
                case 21:
                    break;
                case 12:
                    result.ReanimationId = 39;
                    result.X += 61f;
                    result.Y -= 16f;
                    break;
                case 17:
                    result.ReanimationId = 38;
                    if (backwards)
                    {
                        result.X += 14f;
                    }

                    result.Animation = phase == 38 ? "anim_crumble_noaxe" : "anim_crumble";
                    break;
                case 22:
                    result.ReanimationId = 40;
                    result.X -= 36f;
                    result.Y -= 20f;
                    break;
                case 23:
                case 32:
                    result.ReanimationId = 41;
                    result.X -= 15f;
                    result.Y -= 10f;
                    result.HideImp = !hasObject;
                    break;
                case 24:
                    result.ReanimationId = 37;
                    result.X -= 6f;
                    break;
                default:
                    result.Visual = AdventureBurnVisual.None;
                    return result;
            }

            if (kind == 16)
            {
                result.Y += 31f;
            }

            // PC-only rule: dancing/backup charred bodies restore scale 1, unlike the mobile port.
            result.Scale = kind == 8 || kind == 9 ? 1f : scale;
            result.PivotX = 20f * (1f - result.Scale);
            result.PivotY = 120f * (1f - result.Scale);
            result.FacingLeft = backwards;
            return result;
        }
    }
}
