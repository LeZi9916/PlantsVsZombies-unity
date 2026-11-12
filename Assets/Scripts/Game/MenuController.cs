#nullable enable
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.UI;
using PlantsVsZombies.UI.BitmapFonts;
using System;
using System.IO;
using UnityEngine;

namespace PlantsVsZombies.Game
{
    /// <summary>Opens and closes the profile, options, almanac and unavailable-feature dialogs of the title menus.</summary>
    internal sealed class MenuController
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose menu views this controller drives.</param>
        internal MenuController(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Stores almanac return view.</summary>
        private PvZGame.ScreenView _almanacReturnView = PvZGame.ScreenView.Selector;
        /// <summary>Stores almanac returns to options.</summary>
        private bool _almanacReturnsToOptions;

        /// <summary>Performs initialize profiles.</summary>
        internal void InitializeProfiles()
        {
            try
            {
                var arguments = Environment.GetCommandLineArgs();
                var firstUserSmoke = Array.IndexOf(arguments, "-pvz-first-user-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-first-user-logic-smoke") >= 0;
                var path = _owner._smokeMode ? Path.Combine(Application.temporaryCachePath, "pvz-smoke-" + Guid.NewGuid().ToString("N") + ".dat") : Path.Combine(Application.persistentDataPath, "replica-profiles.dat");
                _owner._profiles = new ProfileCatalog(path);
                _owner._profileDialog = new ProfileDialog(_owner._root, _owner._profiles);
                if (_owner._smokeMode && !firstUserSmoke)
                {
                    _owner._profiles.Create("自动测试");
                }
                else if (_owner._profiles.Profiles.Count == 0)
                {
                    _owner._profileDialog.Open();
                }
            }
            catch (Exception error)
            {
                _owner._profileLoadError = error.Message;
                SetUnavailable("用户目录无法读取", "原文件未更改");
                Debug.LogError("Could not open replica profiles: " + error.Message);
            }
        }

        /// <summary>Performs load bitmap font.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting load bitmap font.</returns>
        internal static CanvasBitmapFont? LoadBitmapFont(string name)
        {
            try
            {
                return CanvasBitmapFont.LoadFromResources(name);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Bitmap font " + name + ": " + error.Message);
                return null;
            }
        }

        /// <summary>Performs set unavailable.</summary>
        /// <param name = "feature">The feature.</param>
        /// <param name = "detail">The detail.</param>
        internal void SetUnavailable(string feature, string? detail)
        {
            _owner._unavailableFeature = feature;
            _owner._unavailableDetail = detail;
        }

        /// <summary>Performs open modes.</summary>
        /// <param name = "category">The category.</param>
        internal void OpenModes(int category)
        {
            _owner.RequiredModeView.Open(category);
            _owner._currentView = PvZGame.ScreenView.Modes;
        }

        /// <summary>Performs open almanac.</summary>
        /// <param name = "returnView">The return view.</param>
        /// <param name = "returnToOptions">The return to options.</param>
        internal void OpenAlmanac(PvZGame.ScreenView returnView, bool returnToOptions = false)
        {
            _almanacReturnView = returnView;
            _almanacReturnsToOptions = returnToOptions;
            _owner.RequiredAlmanacView.Open();
            _owner._currentView = PvZGame.ScreenView.Almanac;
        }

        /// <summary>Performs handle almanac pointer.</summary>
        /// <param name = "point">The point.</param>
        /// <param name = "eventType">The event type.</param>
        /// <returns>The resulting handle almanac pointer.</returns>
        internal bool HandleAlmanacPointer(Vector2 point, PointerPhase eventType)
        {
            var consumed = _owner.RequiredAlmanacView.HandlePointer(point, eventType);
            ReturnFromAlmanacIfClosed();
            return consumed;
        }

        /// <summary>Performs return from almanac if closed.</summary>
        internal void ReturnFromAlmanacIfClosed()
        {
            if (!_owner.RequiredAlmanacView.IsClosed)
            {
                return;
            }

            _owner._currentView = _almanacReturnView;
            if (_almanacReturnsToOptions)
            {
                _owner.RequiredOptionsView.ResumeFromAlmanac();
            }

            _almanacReturnsToOptions = false;
        }

        /// <summary>Performs open options.</summary>
        internal void OpenOptions()
        {
            _owner.RequiredOptionsView.GameplayButtonsEnabled = _owner._currentView == PvZGame.ScreenView.Board;
            _owner.RequiredOptionsView.Open(_owner.Music._musicVolume, _owner.Sounds._soundVolume, Screen.fullScreen);
        }

        /// <summary>Performs close options.</summary>
        internal void CloseOptions()
        {
            _owner.RequiredOptionsView.Close();
            if (!_owner.RequiredOptionsView.IsOpen)
            {
                ApplyOptions();
            }
        }

        /// <summary>Performs apply options.</summary>
        internal void ApplyOptions()
        {
            // The original almanac is modal over the options dialog. Retain its
            // unapplied draft and keep gameplay suspended until Return to Game.
            if (_owner.RequiredOptionsView.CloseResult == OptionsCloseResult.OpenAlmanac)
            {
                OpenAlmanac(_owner._currentView, true);
                return;
            }

            _owner.Music.SetVolume(_owner.RequiredOptionsView.MusicVolume);
            _owner.Sounds.SetVolume(_owner.RequiredOptionsView.SoundVolume);
            _owner.Dave.SetVoiceVolume(_owner.Sounds._soundVolume);
            if (!_owner._smokeMode)
            {
                PlayerPrefs.SetFloat("PvZReplica.Music", _owner.Music._musicVolume);
                PlayerPrefs.SetFloat("PvZReplica.Sound", _owner.Sounds._soundVolume);
                PlayerPrefs.Save();
                if (Screen.fullScreen != _owner.RequiredOptionsView.FullScreen)
                {
                    Screen.fullScreen = _owner.RequiredOptionsView.FullScreen;
                }
            }

            switch (_owner.RequiredOptionsView.CloseResult)
            {
                case OptionsCloseResult.RestartLevel:
                    _owner.ResetGame();
                    break;
                case OptionsCloseResult.MainMenu:
                    _owner._currentView = PvZGame.ScreenView.Selector;
                    break;
            }
        }
    }
}
