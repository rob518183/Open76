using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

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
            return XRSettings.enabled ? "On" : "Off";
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
            XRSettings.enabled = !XRSettings.enabled;
            _menuController.Redraw();
        }

        public void Back()
        {
            _menuController.ShowMenu<OptionsMenu>();
        }
    }
}