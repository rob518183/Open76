using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System.Collections.Generic;

namespace Assets.Scripts.Menus
{
    internal class GraphicsMenu : IMenu
    {
        private MenuController _menuController;
        private Resolution[] _availableResolutions;

        public MenuDefinition BuildMenu(MenuController menuController)
        {
            _menuController = menuController;

            // Cache resolutions
            _availableResolutions = Screen.resolutions
                .Select(r => new { r.width, r.height, r.refreshRateRatio.value }) // Use ratio.value for comparison
                .Distinct() 
                .Select(x => Screen.resolutions.First(r => 
                    r.width == x.width && 
                    r.height == x.height && 
                    r.refreshRateRatio.value == x.value))
                .ToArray();

            return new MenuDefinition
            {
                BackgroundFilename = "6grxdet1",
                MenuItems = new MenuItem[] {
                    new MenuButton("Screen Resolution", GetCurrentResolutionString(), NextResolution),
                    new MenuButton("Quality", GetCurrentQuality(), NextQuality),
                    new MenuButton("Fullscreen", Screen.fullScreen ? "On" : "Off", ToggleFullscreen),
                    new MenuBlank(),
                    new MenuButton("Virtual Reality", GetVRStatus(), ToggleVR),
                    new MenuBlank(),
                    new MenuButton("Cancel", "", Back)
                }
            };
        }

        private string GetVRStatus()
        {
            return IsVREnabled() ? "On" : "Off";
        }

        private string GetCurrentResolutionString()
        {
            Resolution current = Screen.currentResolution;
            // FIXED: Use refreshRateRatio.value. 
            // Formatted to "0.##" to show 59.94Hz correctly, but display 60Hz as just 60.
            return $"{current.width} x {current.height} @ {current.refreshRateRatio.value:0.##}Hz";
        }

        private string GetCurrentQuality()
        {
            int level = QualitySettings.GetQualityLevel();
            return QualitySettings.names[level];
        }

        private void NextResolution()
        {
            int currentIndex = -1;
            Resolution current = Screen.currentResolution;

            for (int i = 0; i < _availableResolutions.Length; i++)
            {
                // Compare dimensions and refresh rate approximately (floating point safety)
                if (_availableResolutions[i].width == current.width && 
                    _availableResolutions[i].height == current.height &&
                    Mathf.Approximately((float)_availableResolutions[i].refreshRateRatio.value, (float)current.refreshRateRatio.value))
                {
                    currentIndex = i;
                    break;
                }
            }

            // If not found (custom window size), default to 0, otherwise increment
            int nextIndex = (currentIndex + 1) % _availableResolutions.Length;
            Resolution newRes = _availableResolutions[nextIndex];

            // FIXED: New SetResolution signature requires FullScreenMode and RefreshRate (struct)
            // We convert the boolean 'Screen.fullScreen' to the appropriate mode.
            FullScreenMode mode = Screen.fullScreen ? Screen.fullScreenMode : FullScreenMode.Windowed;
            
            Screen.SetResolution(newRes.width, newRes.height, mode, newRes.refreshRateRatio);

            _menuController.Redraw();
        }

        private void NextQuality()
        {
            int nextLevel = (QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length;
            QualitySettings.SetQualityLevel(nextLevel, true);
            _menuController.Redraw();
        }

        private void ToggleFullscreen()
        {
            // Simple toggle between Windowed and Exclusive Fullscreen
            // You might prefer FullScreenMode.FullScreenWindow (Borderless) depending on your game style
            Screen.fullScreenMode = !Screen.fullScreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
            _menuController.Redraw();
        }

        private void ToggleVR()
        {
            bool enable = !IsVREnabled();
            SetVREnabled(enable);
            _menuController.Redraw();
        }

        private bool IsVREnabled()
        {
            try
            {
                var displays = new List<XRDisplaySubsystem>();
                UnityEngine.SubsystemManager.GetSubsystems(displays);
                return displays.Exists(d => d != null && d.running);
            }
            catch
            {
                // Fallback to legacy API if subsystems or XR Management aren't available
                return XRSettings.enabled;
            }
        }

        private void SetVREnabled(bool enabled)
        {
            try
            {
                var mgr = XRGeneralSettings.Instance?.Manager;
                if (mgr == null)
                {
                    SetLegacyXREnabled(enabled);
                    return;
                }

                if (enabled)
                {
                    if (mgr.activeLoader == null)
                    {
                        // Initialize loader (async) then start subsystems via coroutine runner
                        _menuController.StartCoroutine(InitializeLoaderAndStart(mgr));
                    }
                    else
                    {
                        mgr.StartSubsystems();
                    }
                }
                else
                {
                    mgr.StopSubsystems();
                    mgr.DeinitializeLoader();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("SetVREnabled failed, falling back to XR display subsystem control: " + ex.Message);
                SetLegacyXREnabled(enabled);
            }
        }

        private void SetLegacyXREnabled(bool enabled)
        {
            try
            {
                var displays = new List<XRDisplaySubsystem>();
                SubsystemManager.GetSubsystems(displays);

                if (enabled)
                {
                    foreach (var display in displays)
                    {
                        display?.Start();
                    }
                }
                else
                {
                    foreach (var display in displays)
                    {
                        display?.Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("SetLegacyXREnabled failed: " + ex.Message);
            }
        }

        private IEnumerator InitializeLoaderAndStart(XRManagerSettings mgr)
        {
            if (mgr == null) yield break;

            // InitializeLoader is implemented as a coroutine in XR Management; yield it if present
            IEnumerator init = null;
            try { init = mgr.InitializeLoader(); } catch { init = null; }

            if (init != null)
            {
                yield return init;
            }
            else
            {
                // Fallback: wait for activeLoader to be set with a timeout
                float timeout = 5f;
                float t = 0f;
                while (mgr.activeLoader == null && t < timeout)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
            }

            if (mgr.activeLoader != null)
            {
                mgr.StartSubsystems();
            }
            else
            {
                Debug.LogWarning("XR loader failed to initialize within timeout.");
            }
        }

        public void Back()
        {
            _menuController.ShowMenu<OptionsMenu>();
        }
    }
}