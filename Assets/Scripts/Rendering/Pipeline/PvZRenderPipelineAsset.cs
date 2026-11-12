#nullable enable
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace PlantsVsZombies.Rendering.Pipeline
{
    /// <summary>Stores the settings of the unlit 2D render pipeline used by every camera in the game.</summary>
    /// <remarks>Shaders written for this pipeline tag their SubShader with <see cref="ShaderTag"/>.</remarks>
    public sealed class PvZRenderPipelineAsset : RenderPipelineAsset<PvZRenderPipeline>
    {
        /// <summary>The SubShader <c>RenderPipeline</c> tag accepted by this pipeline.</summary>
        public const string ShaderTag = "PvZRenderPipeline";

        /// <summary>Gets whether compatible materials are drawn through the SRP Batcher.</summary>
        [field: SerializeField]
        [field: FormerlySerializedAs("UseSrpBatcher")]
        public bool UseSrpBatcher { get; private set; } = true;

        /// <summary>Gets the SubShader tag that selects shaders written for this pipeline.</summary>
        public override string renderPipelineShaderTag => ShaderTag;

        /// <summary>Creates the runtime pipeline instance for the current settings.</summary>
        /// <returns>A new pipeline that renders all cameras until the asset changes.</returns>
        protected override RenderPipeline CreatePipeline()
        {
            return new PvZRenderPipeline(this);
        }
    }
}
