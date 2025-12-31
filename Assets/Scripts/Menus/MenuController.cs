using Assets.Scripts.System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Menus
{
    [RequireComponent(typeof(CanvasGroup))] // Ensure dependency exists
    public class MenuController : MonoBehaviour
    {
        public Button MenuButtonPrefab;
        public Transform BlankSeparatorPrefab;

        public VerticalLayoutGroup Items;
        public RawImage Background;
        private CanvasGroup _canvasGroup;

        private IMenu _currentMenu;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            HideMenuCanvas(); // helper to set alpha and raycasts
        }

        private void Update()
        {
			// 2. Replace legacy Input.GetKeyDown with the new API
            // We check if the Keyboard exists first to avoid null errors on consoles/mobile
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_currentMenu != null)
                    _currentMenu.Back();
                else
                    ShowMenu<OptionsMenu>();
            }
			// Optional: Support Gamepad "Start" or "B" button for back/menu
            if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            {
                 if (_currentMenu != null) _currentMenu.Back();
                 else ShowMenu<OptionsMenu>();
            }
        }

        public void CloseMenu()
        {
            _currentMenu = null;
            HideMenuCanvas();
            Time.timeScale = 1;
        }

        public void ShowMenu<T>() where T : IMenu, new()
        {
            _currentMenu = new T();
            
            // Clear selection so we start fresh or default to top
            EventSystem.current.SetSelectedGameObject(null);

            Redraw();
            ShowMenuCanvas();
            Time.timeScale = 0;
        }

        private void ShowMenuCanvas()
        {
            _canvasGroup.alpha = 1;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true; // Enable clicking
        }

        private void HideMenuCanvas()
        {
            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false; // Disable clicking
        }

        public void Redraw()
        {
            if (_currentMenu == null) return;

            MenuDefinition menuDefinition = _currentMenu.BuildMenu(this);

            // 1. Background Handling
            if (CacheManager.Instance != null)
            {
                Texture2D texture = CacheManager.Instance.GetTexture(menuDefinition.BackgroundFilename);
                if (texture != null)
                {
                    Background.texture = texture;
                    Background.rectTransform.sizeDelta = new Vector2(texture.width, texture.height);
                }
            }

            // 2. Save Selection State
            int selectedIndex = 0;
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                selectedIndex = EventSystem.current.currentSelectedGameObject.transform.GetSiblingIndex();
            }

            // 3. Clean up old items safely
            // Iterate backwards to avoid collection modification issues (though not strictly necessary with Transform enumeration, it's safer)
            foreach (Transform child in Items.transform)
            {
                // CRITICAL FIX: Disable first to prevent LayoutGroup flickering
                child.gameObject.SetActive(false); 
                Destroy(child.gameObject);
            }

            // 4. Instantiate new items
            GameObject objectToSelect = null;

            for (int i = 0; i < menuDefinition.MenuItems.Length; i++)
            {
                MenuItem menuItem = menuDefinition.MenuItems[i];
                if (menuItem is MenuButton)
                {
                    MenuButton menuButton = menuItem as MenuButton;
                    Button button = Instantiate(MenuButtonPrefab, Items.transform);
                    
                    // Safety check for text components
                    var textContainer = button.transform.Find("TextContainer");
                    if(textContainer) textContainer.GetComponentInChildren<Text>().text = menuButton.Text;
                    
                    var valueText = button.transform.Find("Value");
                    if(valueText) valueText.GetComponent<Text>().text = menuButton.Value;

                    button.onClick.AddListener(new UnityEngine.Events.UnityAction(menuButton.OnClick));

                    // Determine if this should be selected
                    if (i == selectedIndex)
                    {
                        objectToSelect = button.gameObject;
                    }
                    // Fallback: If our previous index is now out of bounds (e.g. went from 5 items to 2), select the first available button
                    else if (objectToSelect == null && i == 0)
                    {
                         objectToSelect = button.gameObject;
                    }
                }
                else if (menuItem is MenuBlank)
                {
                    Instantiate(BlankSeparatorPrefab, Items.transform);
                }
            }

            // 5. Restore Selection
            if (objectToSelect != null)
            {
                EventSystem.current.SetSelectedGameObject(objectToSelect);
            }
        }
    }
}