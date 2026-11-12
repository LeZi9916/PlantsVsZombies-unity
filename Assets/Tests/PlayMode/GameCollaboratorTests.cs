#nullable enable
using NUnit.Framework;
using PlantsVsZombies.Diagnostics;
using PlantsVsZombies.Game;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Protects the ownership split between the game coordinator and the collaborators it delegates to.</summary>
    public sealed class GameCollaboratorTests
    {
        /// <summary>Owns the inactive coordinator, whose Awake never runs.</summary>
        private GameObject _host = null!;
        /// <summary>Provides the coordinator under test.</summary>
        private PvZGame _game = null!;

        /// <summary>Creates a coordinator on an inactive object so no original assets or audio are loaded.</summary>
        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("Collaborator fixture");
            _host.SetActive(false);
            _game = _host.AddComponent<PvZGame>();
        }

        /// <summary>Releases the coordinator and every component the fixture attached to it.</summary>
        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_host);
        }

        /// <summary>Requires every collaborator to exist without Awake and to stay the same instance for the whole session.</summary>
        [Test]
        public void EveryCollaboratorIsCreatedOnceWithoutAwake()
        {
            var accessors = CollaboratorProperties();
            Assert.That(accessors.Count, Is.GreaterThanOrEqualTo(15), "The coordinator must keep delegating its responsibilities.");
            foreach (var property in accessors)
            {
                var first = property.GetValue(_game);
                Assert.That(first, Is.Not.Null, property.Name);
                Assert.That(property.GetValue(_game), Is.SameAs(first), property.Name + " must not be recreated.");
            }
        }

        /// <summary>Requires the effects component to adopt its source, apply the volume and prewarm the pooled voices.</summary>
        [Test]
        public void SoundEffectsAdoptTheirSourceAndVolume()
        {
            var source = _host.AddComponent<AudioSource>();
            var before = _host.GetComponents<AudioSource>().Length;
            _game.Sounds.Initialize(source, .25f);
            Assert.That(source.volume, Is.EqualTo(.25f));
            Assert.That(_game.Sounds._soundVolume, Is.EqualTo(.25f));
            Assert.That(_host.GetComponents<AudioSource>().Length, Is.EqualTo(before + 64), "Initialization prewarms the 64 pooled voices.");
            _game.Sounds.SetVolume(.5f);
            Assert.That(source.volume, Is.EqualTo(.5f));
            Assert.That(_game.Sounds._soundVolume, Is.EqualTo(.5f));
        }

        /// <summary>Requires the music player and Dave's voice to adopt their sources and follow later volume changes.</summary>
        [Test]
        public void MusicAndDaveAdoptTheirSourcesAndVolumes()
        {
            var music = _host.AddComponent<AudioSource>();
            var voice = _host.AddComponent<AudioSource>();
            _game.Music.Initialize(music, .35f);
            _game.Dave.Initialize(voice, .7f);
            Assert.That(music.loop, Is.True, "Music loops until the cue changes.");
            Assert.That(music.volume, Is.EqualTo(.35f));
            Assert.That(voice.volume, Is.EqualTo(.7f));
            _game.Music.SetVolume(.1f);
            _game.Dave.SetVoiceVolume(.2f);
            Assert.That(music.volume, Is.EqualTo(.1f));
            Assert.That(_game.Music._musicVolume, Is.EqualTo(.1f));
            Assert.That(voice.volume, Is.EqualTo(.2f));
        }

        /// <summary>Requires named-member lookup to find each member on the object that declares it and to report absent names.</summary>
        [Test]
        public void MemberReflectionFindsMembersOnTheirDeclaringCollaborator()
        {
            Assert.That(GameMemberReflection.FindField(_game, "_currentView", out var viewOwner), Is.Not.Null);
            Assert.That(viewOwner, Is.SameAs(_game));
            Assert.That(GameMemberReflection.FindField(_game, "_presentationSounds", out var soundsOwner), Is.Not.Null);
            Assert.That(soundsOwner, Is.SameAs(_game.Sounds));
            Assert.That(GameMemberReflection.FindMethod(_game, "HandleInput", out var routerOwner), Is.Not.Null);
            Assert.That(routerOwner, Is.SameAs(_game.Router));
            Assert.That(GameMemberReflection.FindField(_game, "_noSuchMember", out _), Is.Null);
            Assert.Throws<MissingMemberException>(() => GameMemberReflection.GetField(_game, "_noSuchMember"));
            GameMemberReflection.SetField(_game, "_soundVolume", .4f);
            Assert.That(_game.Sounds._soundVolume, Is.EqualTo(.4f));
        }

        /// <summary>Lists the internal properties through which the coordinator exposes its collaborators.</summary>
        /// <returns>The collaborator accessors declared by the coordinator.</returns>
        private static List<PropertyInfo> CollaboratorProperties()
        {
            var result = new List<PropertyInfo>();
            foreach (var property in typeof(PvZGame).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (property.PropertyType.IsClass && property.PropertyType.Assembly == typeof(PvZGame).Assembly && !property.Name.StartsWith("Required", StringComparison.Ordinal))
                {
                    result.Add(property);
                }
            }

            return result;
        }
    }
}
