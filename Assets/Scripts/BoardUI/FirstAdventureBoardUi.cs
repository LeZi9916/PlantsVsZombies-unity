using System;

namespace PvZReplica.BoardUI
{
    /// <summary>A top-left-origin point, in native logical pixels.</summary>
    public readonly struct NativePoint
    {
        public readonly int X;
        public readonly int Y;

        public NativePoint(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>A top-left-origin integer rectangle, in native logical pixels.</summary>
    public readonly struct NativeRect
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Width;
        public readonly int Height;

        public NativeRect(int x, int y, int width, int height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    /// <summary>An unscaled image operation before or after logical clipping.</summary>
    public readonly struct NativeBlit
    {
        public readonly NativeRect Source;
        public readonly NativeRect Destination;

        public NativeBlit(NativeRect source, NativeRect destination)
        {
            if (source.Width != destination.Width || source.Height != destination.Height)
                throw new ArgumentException("Only unscaled blits are supported.");
            Source = source;
            Destination = destination;
        }

        public bool TryClip(NativeRect clip, out NativeBlit result)
        {
            long left = Math.Max((long)Destination.X, clip.X);
            long top = Math.Max((long)Destination.Y, clip.Y);
            long right = Math.Min((long)Destination.X + Destination.Width, (long)clip.X + clip.Width);
            long bottom = Math.Min((long)Destination.Y + Destination.Height, (long)clip.Y + clip.Height);
            if (right <= left || bottom <= top)
            {
                result = default(NativeBlit);
                return false;
            }

            int width = checked((int)(right - left));
            int height = checked((int)(bottom - top));
            int sourceX = checked((int)(Source.X + left - Destination.X));
            int sourceY = checked((int)(Source.Y + top - Destination.Y));
            result = new NativeBlit(
                new NativeRect(sourceX, sourceY, width, height),
                new NativeRect(checked((int)left), checked((int)top), width, height));
            return true;
        }
    }

    /// <summary>
    /// Isolated draw coordinates for the examined executable's first-play 1-1
    /// path. Board and seed-bank origins are supplied independently by the host.
    /// These are drawing calls, not a complete renderer or an animation clock.
    /// </summary>
    public static class FirstAdventureBoardUi
    {
        public const int CanvasWidth = 800;
        public const int CanvasHeight = 600;
        public const int BackgroundWidth = 1400;
        public const int BackgroundHeight = 600;
        public const int SodWidth = 771;
        public const int SodHeight = 127;
        public const int SeedBankWidth = 446;
        public const int SeedBankHeight = 87;
        public const int SeedBankEntranceY = -SeedBankHeight;
        public const string SunFontResource = "FONT_CONTINUUMBOLD14";

        public static NativeRect Canvas
        {
            get { return new NativeRect(0, 0, CanvasWidth, CanvasHeight); }
        }

        public static NativeBlit UnsoddedBackground(int boardX, int boardY)
        {
            return Blit(0, 0, BackgroundWidth, BackgroundHeight, checked(boardX - 220), boardY);
        }

        /// <param name="nativeProgress">The original Board+0x556C field, not elapsed seconds.</param>
        public static int RevealedSodWidth(int nativeProgress)
        {
            if (nativeProgress <= 0) return 0;
            if (nativeProgress >= 1000) return SodWidth;
            return (SodWidth * nativeProgress + 500) / 1000;
        }

        public static NativeBlit SingleRowSod(int boardX, int boardY, int nativeProgress)
        {
            return Blit(0, 0, RevealedSodWidth(nativeProgress), SodHeight,
                checked(boardX + 19), checked(boardY + 265));
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
            if (measuredWidth < 0) throw new ArgumentOutOfRangeException(nameof(measuredWidth));
            return new NativePoint(checked(bankX + 38 - measuredWidth / 2), checked(bankY + 78));
        }

        public static int DisplayedSun(int nativeSun)
        {
            return nativeSun > 0 ? nativeSun : 0;
        }

        public static bool IsSunTextRed(int flashCounter)
        {
            return flashCounter > 0 && flashCounter % 20 < 10;
        }

        private static NativeBlit Blit(int sourceX, int sourceY, int width, int height, int x, int y)
        {
            return new NativeBlit(new NativeRect(sourceX, sourceY, width, height),
                new NativeRect(x, y, width, height));
        }
    }
}
