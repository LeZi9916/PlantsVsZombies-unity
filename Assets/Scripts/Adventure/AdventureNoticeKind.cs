#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure notice kind.</summary>
    public enum AdventureNoticeKind
    {
        /// <summary>Identifies tutorial.</summary>
        Tutorial,
        /// <summary>Identifies wave warning.</summary>
        WaveWarning,
        /// <summary>Identifies wave spawned.</summary>
        WaveSpawned,
        /// <summary>Identifies grave rise.</summary>
        GraveRise,
        /// <summary>Identifies pool ambush.</summary>
        PoolAmbush,
        /// <summary>Identifies roof drop.</summary>
        RoofDrop,
        /// <summary>Identifies conveyor packet.</summary>
        ConveyorPacket,
        /// <summary>Identifies vase stage.</summary>
        VaseStage,
        /// <summary>Identifies boss attack.</summary>
        BossAttack,
        /// <summary>Identifies reward dropped.</summary>
        RewardDropped,
        /// <summary>Identifies reward collected.</summary>
        RewardCollected,
        /// <summary>Identifies upgrade planted.</summary>
        UpgradePlanted,
        /// <summary>Identifies imitater morphed.</summary>
        ImitaterMorphed,
        /// <summary>Identifies cob aim started.</summary>
        CobAimStarted,
        /// <summary>Identifies cob fired.</summary>
        CobFired,
        /// <summary>Identifies cob impact.</summary>
        CobImpact,
        /// <summary>Identifies cob ready.</summary>
        CobReady,
        /// <summary>Identifies coin created.</summary>
        CoinCreated,
        /// <summary>Identifies coin collected.</summary>
        CoinCollected,
        /// <summary>Identifies reward fade.</summary>
        RewardFade,
        /// <summary>Identifies the beginning of a manual money pickup and carries its coin kind.</summary>
        CoinCollectionStarted,
        /// <summary>Identifies a failed pickup persistence attempt and carries the retryable coin identifier.</summary>
        CoinCollectionFailed,
        /// <summary>Requests the original adventure vase transition dialogue; the value is its first message index.</summary>
        VaseInterludeStarted
    }
}
