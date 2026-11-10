#nullable enable
using System;

namespace PlantsVsZombies.BoardUI
{
    /// <summary>An unscaled image operation before or after logical clipping.</summary>
    public readonly struct NativeBlit
    {
        /// <summary>Stores source.</summary>
        public readonly NativeRect Source;
        /// <summary>Stores destination.</summary>
        public readonly NativeRect Destination;
        /// <summary>Initializes native blit.</summary>
        /// <param name = "source">The source.</param>
        /// <param name = "destination">The destination.</param>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public NativeBlit(NativeRect source, NativeRect destination)
        {
            if (source.Width != destination.Width || source.Height != destination.Height)
            {
                throw new ArgumentException("Only unscaled blits are supported.");
            }

            Source = source;
            Destination = destination;
        }

        /// <summary>Performs try clip.</summary>
        /// <param name = "clip">The clip.</param>
        /// <param name = "result">The result.</param>
        /// <returns>The resulting try clip.</returns>
        public bool TryClip(NativeRect clip, out NativeBlit result)
        {
            var left = Math.Max((long)Destination.X, clip.X);
            var top = Math.Max((long)Destination.Y, clip.Y);
            var right = Math.Min((long)Destination.X + Destination.Width, (long)clip.X + clip.Width);
            var bottom = Math.Min((long)Destination.Y + Destination.Height, (long)clip.Y + clip.Height);
            if (right <= left || bottom <= top)
            {
                result = default(NativeBlit);
                return false;
            }

            var width = checked((int)(right - left));
            var height = checked((int)(bottom - top));
            var sourceX = checked((int)(Source.X + left - Destination.X));
            var sourceY = checked((int)(Source.Y + top - Destination.Y));
            result = new NativeBlit(new NativeRect(sourceX, sourceY, width, height), new NativeRect(checked((int)left), checked((int)top), width, height));
            return true;
        }
    }
}
