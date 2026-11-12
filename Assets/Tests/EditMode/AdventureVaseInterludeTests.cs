#nullable enable
using System.Collections.Generic;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Exercises both original adventure Vasebreaker stage transitions.</summary>
    public sealed class AdventureVaseInterludeTests
    {
        /// <summary>Checks delayed dialogue, population at the specified entry, and prevention of early victory.</summary>
        [Test]
        public void BothStagesWaitForTheirOriginalDialogueEntries()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 35 }, 7529);
            game.Adventure.VaseInterludeDialogueEnabled = true;
            var messages = new List<int>();
            game.Adventure.NoticeRaised = notice =>
            {
                if (notice.Kind == AdventureNoticeKind.VaseInterludeStarted)
                {
                    messages.Add(notice.Value);
                }
            };

            for (var stage = 0; stage < 2; stage++)
            {
                game.Adventure._vases.Clear();
                game.Update(.01f);
                Assert.That(game.Adventure.VaseInterludeActive, Is.True);
                game.Update(1.99f);
                Assert.That(messages.Count, Is.EqualTo(stage));
                Assert.That(game.Adventure.VaseStage, Is.EqualTo(stage));
                game.Update(.01f);
                Assert.That(messages[stage], Is.EqualTo(stage == 0 ? 2700 : 2800));
                Assert.That(game.Adventure.VaseStage, Is.EqualTo(stage + 1));
                Assert.That(game.Adventure.Vases, Is.Empty);
                Assert.That(game.State, Is.EqualTo(MatchState.Playing));
                Assert.That(game.FinishAdventureVaseInterlude(), Is.False);
                Assert.That(game.PopulateAdventureVaseInterlude(stage == 0 ? 2701 : 2800), Is.False);
                Assert.That(game.PopulateAdventureVaseInterlude(stage == 0 ? 2702 : 2801), Is.True);
                Assert.That(game.Adventure.Vases, Is.Not.Empty);
                var vase = game.Adventure.Vases[0];
                Assert.That(game.SmashAdventureVase(vase.Row, vase.Column), Is.False);
                Assert.That(game.PopulateAdventureVaseInterlude(stage == 0 ? 2702 : 2801), Is.False);
                Assert.That(game.FinishAdventureVaseInterlude(), Is.True);
            }

            game.Adventure._vases.Clear();
            game.Update(.01f);
            Assert.That(game.State, Is.EqualTo(MatchState.Won));
        }

        /// <summary>Checks headless simulations continue without a presentation host.</summary>
        [Test]
        public void HeadlessSimulationContinuesAfterTheStageDelay()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 35 }, 7529);
            game.Adventure._vases.Clear();
            game.Update(.01f);
            game.Update(2f);
            Assert.That(game.Adventure.VaseStage, Is.EqualTo(1));
            Assert.That(game.Adventure.Vases, Is.Not.Empty);
            Assert.That(game.Adventure.VaseInterludeActive, Is.False);
            Assert.That(game.State, Is.EqualTo(MatchState.Playing));
        }
    }
}
