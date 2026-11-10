#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure intro blit.</summary>
    public readonly struct AdventureIntroBlit
    {
        /// <summary>Stores resource.</summary>
        public readonly string Resource;
        /// <summary>Stores source x, source y, width, height, x, y.</summary>
        public readonly int SourceX, SourceY, Width, Height, X, Y;
        /// <summary>Initializes adventure intro blit.</summary>
        /// <param name = "resource">The resource.</param>
        /// <param name = "sx">The sx.</param>
        /// <param name = "sy">The sy.</param>
        /// <param name = "width">The width.</param>
        /// <param name = "height">The height.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        public AdventureIntroBlit(string resource, int sx, int sy, int width, int height, int x, int y)
        {
            Resource = resource;
            SourceX = sx;
            SourceY = sy;
            Width = width;
            Height = height;
            X = x;
            Y = y;
        }
    }
}
