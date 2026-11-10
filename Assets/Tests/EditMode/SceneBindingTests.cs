#nullable enable

using NUnit.Framework;
using PlantsVsZombies.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Protects serialized scene bindings when script namespaces and assemblies change.</summary>
    public sealed class SceneBindingTests
    {
        /// <summary>Loads the shipped scene and verifies its game component resolves without missing scripts.</summary>
        [Test]
        public void LawnSceneResolvesGameComponent()
        {
            const string scenePath = "Assets/Scenes/Lawn.unity";
            var existing = SceneManager.GetSceneByPath(scenePath);
            var alreadyLoaded = existing.IsValid() && existing.isLoaded;
            var scene = alreadyLoaded ? existing : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                Assert.That(scene.isLoaded, Is.True);
                var games = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    games += root.GetComponentsInChildren<PvZGame>(true).Length;
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    {
                        Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject),
                            Is.Zero, transform.name + " contains a missing script.");
                    }
                }

                Assert.That(games, Is.EqualTo(1), "The serialized Lawn game component must resolve to the migrated runtime type.");
            }
            finally
            {
                if (!alreadyLoaded && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
    }
}
