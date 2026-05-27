using Assets.Scripts.System;
using System;

namespace Assets.Scripts.Menus
{
    internal class AudioControlMenu : IMenu
    {
        private MenuController _menuController;

        public MenuDefinition BuildMenu(MenuController menuController)
        {
            _menuController = menuController;

            return new MenuDefinition
            {
                BackgroundFilename = "6audcon1",
                MenuItems = new MenuItem[] {
                    new MenuBlank(),
                    new MenuButton("Music Level", AudioSettings.FormatDisplay(AudioSettings.MusicLevel), IncreaseMusicLevel),
                    new MenuBlank(),
                    new MenuBlank(),
                    new MenuButton("SFX Level", AudioSettings.FormatDisplay(AudioSettings.SfxLevel), IncreaseSfxLevel),
                    new MenuBlank(),
                    new MenuBlank(),
                    new MenuButton("Voice Level", AudioSettings.FormatDisplay(AudioSettings.VoiceLevel), IncreaseVoiceLevel),
                    new MenuBlank(),
                    new MenuBlank(),
                    new MenuButton("Back", "", Back)
                }
            };
        }

        private void IncreaseMusicLevel()
        {
            AudioSettings.SetMusicLevel(AudioSettings.NextLevel(AudioSettings.MusicLevel));
            _menuController.Redraw();
        }

        private void IncreaseSfxLevel()
        {
            AudioSettings.SetSfxLevel(AudioSettings.NextLevel(AudioSettings.SfxLevel));
            _menuController.Redraw();
        }

        private void IncreaseVoiceLevel()
        {
            AudioSettings.SetVoiceLevel(AudioSettings.NextLevel(AudioSettings.VoiceLevel));
            _menuController.Redraw();
        }

        public void Back()
        {
            _menuController.ShowMenu<OptionsMenu>();
        }
    }
}
