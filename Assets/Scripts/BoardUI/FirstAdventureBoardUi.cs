#nullable enable
using System;

namespace PlantsVsZombies.BoardUI
{
    /// <summary>
    /// Isolated draw coordinates for the examined executable's first-play 1-1
    /// path. Board and seed-bank origins are supplied independently by the host.
    /// These are drawing calls, not a complete renderer or an animation clock.
    /// </summary>
    public static class FirstAdventureBoardUi
    {
        /// <summary>Stores canvas width.</summary>
        public const int CanvasWidth = 800;
        /// <summary>Stores canvas height.</summary>
        public const int CanvasHeight = 600;
        /// <summary>Stores background width.</summary>
        public const int BackgroundWidth = 1400;
        /// <summary>Stores background height.</summary>
        public const int BackgroundHeight = 600;
        /// <summary>Stores sod width.</summary>
        public const int SodWidth = 771;
        /// <summary>Stores sod height.</summary>
        public const int SodHeight = 127;
        /// <summary>Stores seed bank width.</summary>
        public const int SeedBankWidth = 446;
        /// <summary>Stores seed bank height.</summary>
        public const int SeedBankHeight = 87;
        /// <summary>Stores seed bank entrance y.</summary>
        public const int SeedBankEntranceY = -SeedBankHeight;
        /// <summary>Stores sun font resource.</summary>
        public const string SunFontResource = "FONT_CONTINUUMBOLD14";
        /// <summary>Gets or sets canvas.</summary>
        public static NativeRect Canvas
        {
            get
            {
                return new NativeRect(0, 0, CanvasWidth, CanvasHeight);
            }
        }

        /// <summary>Performs unsodded background.</summary>
        /// <param name = "boardX">The board x.</param>
        /// <param name = "boardY">The board y.</param>
        /// <returns>The resulting unsodded background.</returns>
        public static NativeBlit UnsoddedBackground(int boardX, int boardY)
        {
            return Blit(0, 0, BackgroundWidth, BackgroundHeight, checked(boardX - 220), boardY);
        }

        /// <param name = "nativeProgress">The original Board+0x556C field, not elapsed seconds.</param>
        public static int RevealedSodWidth(int nativeProgress)
        {
            if (nativeProgress <= 0)
            {
                return 0;
            }

            if (nativeProgress >= 1000)
            {
                return SodWidth;
            }

            return (SodWidth * nativeProgress + 500) / 1000;
        }

        /// <summary>Performs single row sod.</summary>
        /// <param name = "boardX">The board x.</param>
        /// <param name = "boardY">The board y.</param>
        /// <param name = "nativeProgress">The native progress.</param>
        /// <returns>The resulting single row sod.</returns>
        public static NativeBlit SingleRowSod(int boardX, int boardY, int nativeProgress)
        {
            return Blit(0, 0, RevealedSodWidth(nativeProgress), SodHeight, checked(boardX + 19), checked(boardY + 265));
        }

        /// <summary>
        /// A separate full-image, colorized overlay when Board+0x559C is 2.
        /// The caller supplies the native-equivalent color and compositing state.
        /// </summary>
        public static NativeBlit TutorialSodOverlay(int boardX, int boardY)
        {
            return Blit(0, 0, SodWidth, SodHeight, checked(boardX + 19), checked(boardY + 265));
        }

        /// <summary>The full base image; one packet does not shorten this draw call.</summary>
        public static NativeBlit SeedBank(int bankX, int bankY)
        {
            return Blit(0, 0, SeedBankWidth, SeedBankHeight, bankX, bankY);
        }

        /// <summary>The second, overlapping end-cap draw in the ordinary one-slot path.</summary>
        public static NativeBlit SeedBankEndCap(int bankX, int bankY)
        {
            return Blit(434, 0, 12, SeedBankHeight, checked(bankX + 434), bankY);
        }

        /// <summary>Object bounds for the first seed slot, not its opaque pixel bounds.</summary>
        public static NativeRect FirstSeedSlot(int bankX, int bankY)
        {
            return new NativeRect(checked(bankX + 85), checked(bankY + 8), 50, 70);
        }

        /// <summary>
        /// The draw-call anchor and the left origin after native integer centering.
        /// Width must come from the native-equivalent font measurement; Y is not
        /// asserted to be the top edge of the visible glyphs.
        /// </summary>
        public static NativePoint SunTextOrigin(int bankX, int bankY, int measuredWidth)
        {
            if (measuredWidth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(measuredWidth));
            }

            return new NativePoint(checked(bankX + 38 - measuredWidth / 2), checked(bankY + 78));
        }

        /// <summary>Performs displayed sun.</summary>
        /// <param name = "nativeSun">The native sun.</param>
        /// <returns>The resulting displayed sun.</returns>
        public static int DisplayedSun(int nativeSun)
        {
            return nativeSun > 0 ? nativeSun : 0;
        }

        /// <summary>Performs is sun text red.</summary>
        /// <param name = "flashCounter">The flash counter.</param>
        /// <returns>The resulting is sun text red.</returns>
        public static bool IsSunTextRed(int flashCounter)
        {
            return flashCounter > 0 && flashCounter % 20 < 10;
        }

        /// <summary>Performs blit.</summary>
        /// <param name = "sourceX">The source x.</param>
        /// <param name = "sourceY">The source y.</param>
        /// <param name = "width">The width.</param>
        /// <param name = "height">The height.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <returns>The resulting blit.</returns>
        private static NativeBlit Blit(int sourceX, int sourceY, int width, int height, int x, int y)
        {
            return new NativeBlit(new NativeRect(sourceX, sourceY, width, height), new NativeRect(x, y, width, height));
        }
    }
}
