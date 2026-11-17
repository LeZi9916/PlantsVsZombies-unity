#nullable enable
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Retains the prepared atlas pages of one stage without changing ordinary frame-based retirement.</summary>
    internal sealed class SpritePageLease : IDisposable
    {
        /// <summary>The shared provider acquisition owned until disposal.</summary>
        private SpriteAssetProvider? _owner;
        /// <summary>The logical images that must be rebound after complete-layout fallback.</summary>
        internal readonly HashSet<SpriteImageBinding> Bindings = new HashSet<SpriteImageBinding>();
        /// <summary>The page generations already referenced by this lease.</summary>
        internal readonly HashSet<(int Layout, int Page)> Pages = new HashSet<(int, int)>();

        /// <summary>Creates a stage lease for one shared provider.</summary>
        /// <param name="owner">The provider that owns all leased bindings.</param>
        internal SpritePageLease(SpriteAssetProvider owner)
        {
            _owner = owner;
        }

        /// <summary>Pins the current pages of a logical image and retains its source identity across fallback.</summary>
        /// <param name="provider">The image's actual provider.</param>
        /// <param name="binding">The stable logical source binding.</param>
        /// <exception cref="ObjectDisposedException">The stage lease has already ended.</exception>
        /// <exception cref="ArgumentException">The image belongs to another provider.</exception>
        internal void Pin(SpriteAssetProvider provider, SpriteImageBinding binding)
        {
            if (_owner == null)
            {
                throw new ObjectDisposedException(nameof(SpritePageLease));
            }
            if (!ReferenceEquals(_owner, provider))
            {
                throw new ArgumentException("An atlas stage lease can retain only images from its owning provider.", nameof(provider));
            }
            Bindings.Add(binding);
            provider.PinLeaseImage(this, binding);
        }

        /// <summary>Releases this stage's independent page references once.</summary>
        public void Dispose()
        {
            var owner = _owner;
            if (owner == null)
            {
                return;
            }
            _owner = null;
            owner.ReleasePageLease(this);
            Bindings.Clear();
            Pages.Clear();
        }
    }
}
