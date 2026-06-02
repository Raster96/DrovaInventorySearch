using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Il2CppDrova;
using Il2CppDrova.GUI;
using Il2CppDrova.InventorySystem;
using Il2CppDrova.Items;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using Il2CppInterop.Runtime.InteropTypes;
using Drova_Modding_API.Access;
using Il2CppCustomFramework.Localization;

[assembly: MelonInfo(typeof(DrovaInventorySearch.InventorySearchMod), "Drova Inventory Search", "1.0.0", "Raster96")]
[assembly: MelonGame("Just2D", "Drova")]
[assembly: MelonAdditionalDependencies("Drova_Modding_API")]

namespace DrovaInventorySearch
{
    public class InventorySearchMod : MelonMod
    {
        private GameObject? _searchInputObject;
        private TMP_InputField? _searchInput;
        private GUI_Window? _currentInventoryWindow;
        private string _currentSearchText = string.Empty;
        private bool _isSearchActive = false;
        private bool _diagnosticsDone = false;
        private bool _mainInventoryDiagnosticsDone = false;
        private UnityEngine.UI.Button? _allCategoryButton = null;
        private readonly List<UnityEngine.UI.Button> _categoryButtons = new();
        private readonly Dictionary<UnityEngine.UI.Button, UnityEngine.Events.UnityAction> _categoryButtonListeners = new();
        private bool _isChangingCategoryProgrammatically = false;
        private bool _localizationInitialized = false;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("=== Drova Inventory Search v1.1 ===");
            // Localization will be initialized later when LocalizationDB is available
        }

        private void InitializeLocalization()
        {
            try
            {
                var entries = new List<LocalizationAccess.LocalizationEntry>();

                var languages = Enum.GetValues(typeof(LocalizationDB.ELanguage));
                
                foreach (LocalizationDB.ELanguage language in languages)
                {
                    string langCode = language.ToString().ToLower();
                    
                    string searchText = langCode switch
                    {
                        "pl" => "Szukaj...",
                        "de" => "Suchen...",
                        "fr" => "Rechercher...",
                        "es" => "Buscar...",
                        "zh_cn" => "搜索...",
                        "zh_tw" => "搜索...",
                        "ko" => "검색...",
                        _ => "Search..."
                    };
                    
                    entries.Add(new LocalizationAccess.LocalizationEntry("InventorySearchPlaceholder", searchText, language));
                }

                LocalizationAccess.CreateLocalizationEntries(entries, "InventorySearch");
                _localizationInitialized = true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[Inventory Search] Failed to initialize localization: {ex.Message}");
            }
        }

        public override void OnUpdate()
        {
            try
            {
                if (_currentInventoryWindow == null)
                {
                    GameObject inventoryObj = GameObject.Find("SceneRoot/GUI_PlayerGameMenu(Clone)")
                                          ?? GameObject.Find("GUI_PlayerGameMenu(Clone)");

                    if (inventoryObj != null && inventoryObj.activeInHierarchy)
                    {
                        var window = inventoryObj.GetComponent<GUI_Window>();
                        if (window != null)
                        {
                            _currentInventoryWindow = window;
                            CreateSearchBar();
                        }
                    }
                }
                else if (!_currentInventoryWindow.gameObject.activeInHierarchy)
                {
                    CleanupSearch();
                }
                else
                {
                    // Check if GUI_MainInventory panel is active
                    UpdateSearchBarVisibility();
                    
                    // Ctrl+F activates the search bar
                    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F))
                    {
                        if (_searchInput != null && _searchInput.gameObject.activeInHierarchy)
                        {
                            _searchInput.ActivateInputField();
                            _searchInput.Select();
                        }
                    }
                }
            }
            catch { }
        }

        private void CreateSearchBar()
        {
            try
            {
                if (_searchInputObject != null) return;
                if (_currentInventoryWindow == null) return;

                // Initialize localization on first search bar creation (LocalizationDB is now ready)
                if (!_localizationInitialized)
                {
                    InitializeLocalization();
                }

                Transform inventoryTransform = _currentInventoryWindow.transform.Find("Panel/GUI_MainInventory/Inventory");
                
                Transform currencyShardsTransform = inventoryTransform?.Find("CurrencyShards");
                
                Transform parentTransform = inventoryTransform;
                Vector2 searchPosition = new Vector2(-200, -15);
                
                if (currencyShardsTransform != null)
                {
                    RectTransform currencyRect = currencyShardsTransform.GetComponent<RectTransform>();
                    if (currencyRect != null)
                    {
                        searchPosition = new Vector2(-75, 7);
                    }
                }

                _searchInputObject = new GameObject("InventorySearchInput");
                _searchInputObject.transform.SetParent(parentTransform, false);

                RectTransform searchRect = _searchInputObject.AddComponent<RectTransform>();
                searchRect.anchorMin = new Vector2(1f, 0f); // Anchor to bottom-right
                searchRect.anchorMax = new Vector2(1f, 0f);
                searchRect.pivot = new Vector2(1f, 0f);
                searchRect.anchoredPosition = searchPosition;
                searchRect.sizeDelta = new Vector2(70, 10); // Same size as gold counter

                // NO Image component - fully transparent background
                // Create border using 4 separate Image lines
                CreateBorderLine(_searchInputObject.transform, "BorderTop", 
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, 0), 
                    new Vector2(70, 1)); // Top border
                CreateBorderLine(_searchInputObject.transform, "BorderBottom", 
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 0), 
                    new Vector2(70, 1)); // Bottom border
                CreateBorderLine(_searchInputObject.transform, "BorderLeft", 
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0, 0), 
                    new Vector2(1, 10)); // Left border
                CreateBorderLine(_searchInputObject.transform, "BorderRight", 
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0, 0), 
                    new Vector2(1, 10)); // Right border

                // Add TMP_InputField
                _searchInput = _searchInputObject.AddComponent<TMP_InputField>();
                
                // Get or add Image component for InputField (but make it transparent)
                Image bgImage = _searchInputObject.GetComponent<Image>();
                if (bgImage == null)
                {
                    bgImage = _searchInputObject.AddComponent<Image>();
                }
                bgImage.color = new Color(0f, 0f, 0f, 0f); // Fully transparent
                bgImage.raycastTarget = true;

                GameObject textArea = new GameObject("TextArea");
                textArea.transform.SetParent(_searchInputObject.transform, false);
                RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
                textAreaRect.anchorMin = Vector2.zero;
                textAreaRect.anchorMax = Vector2.one;
                textAreaRect.offsetMin = new Vector2(5, 2);
                textAreaRect.offsetMax = new Vector2(-5, -2);

                // === PLACEHOLDER ===
                GameObject placeholderObj = new GameObject("Placeholder");
                placeholderObj.transform.SetParent(textArea.transform, false);
                RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
                placeholderRect.anchorMin = Vector2.zero;
                placeholderRect.anchorMax = Vector2.one;
                placeholderRect.offsetMin = Vector2.zero;
                placeholderRect.offsetMax = Vector2.zero;
                TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
                
                // Get current language and use appropriate placeholder text
                string localizedPlaceholder = "Search..."; // Default
                if (_localizationInitialized)
                {
                    try
                    {
                        var currentLanguage = LocalizationDB.Instance.CurrentLanguage;
                        string langCode = currentLanguage.ToString().ToLower();
                        
                        localizedPlaceholder = langCode switch
                        {
                            "pl" => "Szukaj...",
                            "de" => "Suchen...",
                            "fr" => "Rechercher...",
                            "es" => "Buscar...",
                            "zh_cn" => "搜索...",
                            "zh_tw" => "搜索...",
                            "ko" => "검색...",
                            _ => "Search..."
                        };
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error($"[Inventory Search] Failed to get language: {ex.Message}");
                        localizedPlaceholder = "Search...";
                    }
                }
                placeholderText.text = localizedPlaceholder;
                
                placeholderText.fontSize = 7; // Smaller font for smaller box
                placeholderText.color = new Color(0.800f, 0.753f, 0.639f, 0.5f);
                placeholderText.alignment = TextAlignmentOptions.Center;

                // === INPUT TEXT ===
                GameObject inputTextObj = new GameObject("Text");
                inputTextObj.transform.SetParent(textArea.transform, false);
                RectTransform inputTextRect = inputTextObj.AddComponent<RectTransform>();
                inputTextRect.anchorMin = Vector2.zero;
                inputTextRect.anchorMax = Vector2.one;
                inputTextRect.offsetMin = Vector2.zero;
                inputTextRect.offsetMax = Vector2.zero;
                TextMeshProUGUI inputText = inputTextObj.AddComponent<TextMeshProUGUI>();
                inputText.text = "";
                inputText.fontSize = 7; // Smaller font for smaller box
                inputText.color = new Color(0.800f, 0.753f, 0.639f, 1.000f);
                inputText.alignment = TextAlignmentOptions.Center;
                
                // Find Philosopher font
                try
                {
                    var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                    foreach (var font in allFonts)
                    {
                        if (font.name == "Philosopher-Regular SDF")
                        {
                            inputText.font = font;
                            placeholderText.font = font;
                            break;
                        }
                    }
                }
                catch { }

                _searchInput.textViewport = textAreaRect;
                _searchInput.textComponent = inputText;
                _searchInput.placeholder = placeholderText;
                _searchInput.fontAsset = inputText.font;
                _searchInput.interactable = true;
                
                // Use SingleLine so ESC natively deactivates (blurs) the input field
                _searchInput.lineType = TMP_InputField.LineType.SingleLine;
                _searchInput.contentType = TMP_InputField.ContentType.Standard;
                _searchInput.shouldActivateOnSelect = true;
                _searchInput.restoreOriginalTextOnEscape = true;
                
                // Caret settings
                _searchInput.caretWidth = 1;
                _searchInput.customCaretColor = false;
                _searchInput.caretColor = new Color(0.800f, 0.753f, 0.639f, 1.000f);
                _searchInput.selectionColor = new Color(0.659f, 0.808f, 1.000f, 0.753f);

                _searchInput.onValueChanged.AddListener(new Action<string>(OnSearchTextChanged));
                
                // When input field is deactivated (ESC, Enter, click outside),
                // clear EventSystem selection so game keyboard controls work again
                _searchInput.onEndEdit.AddListener(new Action<string>(OnSearchEndEdit));
                
                // Force the input field to initialize its caret properly
                // This ensures the caret is visible immediately when the user focuses
                _searchInput.enabled = false;
                _searchInput.enabled = true;
                _searchInput.ForceLabelUpdate();
                
                UpdateSearchBarVisibility();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[Inventory Search] CreateSearchBar error: {ex.Message}");
            }
        }

        private void CreateBorderLine(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, 
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject borderLine = new GameObject(name);
            borderLine.transform.SetParent(parent, false);
            
            RectTransform rectTransform = borderLine.AddComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            
            Image lineImage = borderLine.AddComponent<Image>();
            lineImage.color = new Color(0.55f, 0.4f, 0.25f, 1f); // Brown color
            lineImage.raycastTarget = false;
        }

        private void UpdateSearchBarVisibility()
        {
            if (_searchInputObject == null || _currentInventoryWindow == null) return;

            try
            {
                // Find GUI_MainInventory panel
                Transform panelTransform = _currentInventoryWindow.transform.Find("Panel");
                if (panelTransform == null) return;

                Transform mainInventoryTransform = panelTransform.Find("GUI_MainInventory");
                if (mainInventoryTransform == null) return;

                GameObject mainInventoryPanel = mainInventoryTransform.gameObject;
                bool isMainInventoryActive = mainInventoryPanel.activeInHierarchy;

                // Run diagnostics on GUI_MainInventory component once
                if (isMainInventoryActive && !_mainInventoryDiagnosticsDone)
                {
                    RunMainInventoryDiagnostics(mainInventoryPanel);
                    _mainInventoryDiagnosticsDone = true;
                }

                // Show search bar only when GUI_MainInventory is active
                if (_searchInputObject.activeSelf != isMainInventoryActive)
                {
                    _searchInputObject.SetActive(isMainInventoryActive);
                    
                    // If hiding, clear the search and restore view
                    if (!isMainInventoryActive && _isSearchActive)
                    {
                        _currentSearchText = string.Empty;
                        if (_searchInput != null)
                        {
                            _searchInput.text = string.Empty;
                        }
                        RestoreOriginalView();
                    }
                }
            }
            catch { }
        }

        private void OnSearchEndEdit(string text)
        {
            // If ESC was pressed, clear the search text
            if (_searchInput != null && _searchInput.wasCanceled)
            {
                _searchInput.text = string.Empty;
                _currentSearchText = string.Empty;
                RestoreOriginalView();
            }
            
            // Clear EventSystem selection so game keyboard controls (ESC, I, etc.) work again
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        private void OnSearchTextChanged(string newText)
        {
            _currentSearchText = newText;
            
            // If user starts typing (non-empty search), switch to "All" category
            if (!string.IsNullOrWhiteSpace(newText) && _allCategoryButton != null)
            {
                try
                {
                    _isChangingCategoryProgrammatically = true;
                    _allCategoryButton.onClick?.Invoke();
                    _isChangingCategoryProgrammatically = false;
                }
                catch
                {
                    _isChangingCategoryProgrammatically = false;
                }
            }
            
            try { ApplySearch(); }
            catch (Exception ex) { MelonLogger.Error($"[Inventory Search] Search error: {ex.Message}"); }
        }

        private char ValidateInput(string text, int charIndex, char addedChar)
        {
            // Block Enter and Return keys from being processed
            if (addedChar == '\n' || addedChar == '\r')
            {
                return '\0'; // Return null character to ignore the input
            }
            return addedChar; // Allow all other characters
        }

        private void OnCategoryButtonClicked(bool isAllButton)
        {
            try
            {
                // Don't clear search if we programmatically clicked the All button
                if (_isChangingCategoryProgrammatically)
                {
                    return;
                }
                
                // If user clicked a category button (not All) while search is active, clear the search
                if (!isAllButton && _isSearchActive && !string.IsNullOrWhiteSpace(_currentSearchText))
                {
                    _currentSearchText = string.Empty;
                    if (_searchInput != null)
                    {
                        // Clear the text field and deactivate it to ensure clean state
                        _searchInput.text = string.Empty;
                        _searchInput.DeactivateInputField();
                    }
                    RestoreOriginalView();
                }
            }
            catch { }
        }

        private void ApplySearch()
        {
            if (_currentInventoryWindow == null) return;

            if (string.IsNullOrWhiteSpace(_currentSearchText))
            {
                if (_isSearchActive) RestoreOriginalView();
                return;
            }

            _isSearchActive = true;
            FilterInventoryItems(_currentSearchText.ToLower());
        }

        private void FilterInventoryItems(string searchText)
        {
            if (_currentInventoryWindow == null) return;

            var slots = _currentInventoryWindow.gameObject
                .GetComponentsInChildren<GUI_InventorySlot>(true);

            int shown = 0, hidden = 0, excluded = 0;

            foreach (var slot in slots)
            {
                if (slot == null) continue;

                // Check if this is an equipment slot (left panel) - these should always be visible
                bool isEquipmentSlot = IsEquipmentSlot(slot);
                
                if (isEquipmentSlot)
                {
                    // Equipment slots are always shown
                    slot.gameObject.SetActive(true);
                    excluded++;
                    continue;
                }

                // For inventory slots (right panel), apply search filter
                string itemName = GetItemName(slot);
                bool matches = string.IsNullOrWhiteSpace(searchText)
                               || itemName.ToLower().Contains(searchText);

                // Hide non-matching items completely to compact the layout
                slot.gameObject.SetActive(matches);

                if (matches) shown++; else hidden++;
            }
        }

        // Check if a slot is an equipment slot (left panel) or hotbar slot vs inventory slot (right panel)
        private bool IsEquipmentSlot(GUI_InventorySlot slot)
        {
            try
            {
                // Equipment slots typically have names like:
                // "GUI_InventorySlot_EquipmentSlot_Head", "GUI_InventorySlot_EquipmentSlot_Body", etc.
                // or they might be under a specific parent object
                
                string slotName = slot.gameObject.name;
                
                // Check if the slot name contains "Equipment" or "EquipmentSlot"
                if (slotName.Contains("Equipment", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                
                // Alternative: Check parent hierarchy for equipment panel or hotbar/quickslot panel
                Transform parent = slot.transform.parent;
                while (parent != null)
                {
                    string parentName = parent.gameObject.name;
                    
                    // Check for equipment area
                    if (parentName.Contains("Equipment", StringComparison.OrdinalIgnoreCase) ||
                        parentName.Contains("Wyposażenie", StringComparison.OrdinalIgnoreCase) ||
                        parentName.Contains("Equipped", StringComparison.OrdinalIgnoreCase) ||
                        parentName.Equals("GUI_EquipArea", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    
                    // Check for hotbar/quickslot area (Pasek skrótów)
                    if (parentName.Contains("Hotbar", StringComparison.OrdinalIgnoreCase) ||
                        parentName.Contains("QuickSlot", StringComparison.OrdinalIgnoreCase) ||
                        parentName.Contains("Shortcut", StringComparison.OrdinalIgnoreCase) ||
                        parentName.Contains("Skrót", StringComparison.OrdinalIgnoreCase) ||
                        parentName.Contains("Pasek", StringComparison.OrdinalIgnoreCase) ||
                        parentName.Equals("GUI_Hotbar", StringComparison.OrdinalIgnoreCase) ||
                        parentName.Equals("GUI_QuickslotArea", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    
                    parent = parent.parent;
                    
                    // Don't go too far up the hierarchy
                    if (parent == _currentInventoryWindow.transform)
                        break;
                }
            }
            catch { }
            
            return false;
        }

        private string GetItemName(GUI_InventorySlot slot)
        {
            if (!_diagnosticsDone)
            {
                try
                {
                    var itemForDiag = slot.Item;
                    if (itemForDiag != null)
                    {
                        LogItemDiagnostics(itemForDiag);
                        _diagnosticsDone = true;
                    }
                }
                catch
                {
                    _diagnosticsDone = true;
                }
            }
            
            // Strategy 1: Get Item and use GetLocalizedItemName method
            try
            {
                var item = slot.Item;
                if (item != null)
                {
                    // Use GetLocalizedItemName method to get the translated name
                    try
                    {
                        var itemType = item.GetType();
                        var getLocalizedMethod = itemType.GetMethod("GetLocalizedItemName");
                        if (getLocalizedMethod != null)
                        {
                            // Call GetLocalizedItemName(false) - false means don't include richtext
                            var translatedName = getLocalizedMethod.Invoke(item, new object[] { false });
                            if (translatedName != null)
                            {
                                string translated = translatedName.ToString();
                                if (!string.IsNullOrEmpty(translated) && !translated.StartsWith("[LOCA]"))
                                {
                                    return translated;
                                }
                            }
                        }
                    }
                    catch { }
                    
                    // Fallback: Try to find DisplayName, LocalizedName, or similar property on Item
                    var itemType2 = item.GetType();
                    
                    // Common display name property patterns
                    string[] displayNameProperties = {
                        "DisplayName", "_displayName", "displayName",
                        "LocalizedName", "_localizedName", "localizedName",
                        "ItemName", "_itemName", "itemName"
                    };
                    
                    foreach (var propName in displayNameProperties)
                    {
                        var prop = itemType2.GetProperty(propName, 
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (prop != null)
                        {
                            var value = prop.GetValue(item);
                            if (value != null)
                            {
                                string displayName = value.ToString();
                                if (!string.IsNullOrEmpty(displayName) && 
                                    !displayName.StartsWith("[LOCA]") &&
                                    displayName != item.name)
                                {
                                    return displayName;
                                }
                            }
                        }
                    }
                    
                    // If item has _localizedString property, try to get the translated text
                    var localizedStringProp = itemType2.GetProperty("_localizedString", 
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (localizedStringProp != null)
                    {
                        var localizedStringObj = localizedStringProp.GetValue(item);
                        if (localizedStringObj != null)
                        {
                            // Try to call GetLocalizedString on it
                            var getLocalizedMethod2 = localizedStringObj.GetType().GetMethod("GetLocalizedString");
                            if (getLocalizedMethod2 != null)
                            {
                                var translated = getLocalizedMethod2.Invoke(localizedStringObj, new object[] { null });
                                if (translated != null)
                                {
                                    string translatedStr = translated.ToString();
                                    if (!string.IsNullOrEmpty(translatedStr) && !translatedStr.StartsWith("[LOCA]"))
                                    {
                                        return translatedStr;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            try
            {
                var name = TryGetItemNameViaProperties(slot);
                if (!string.IsNullOrEmpty(name) && name != "unknown") return name;
            }
            catch { }

            try
            {
                var name = TryGetItemNameNative(slot);
                if (!string.IsNullOrEmpty(name) && name != "unknown") return name;
            }
            catch { }

            try
            {
                var name = TryGetSpriteNameFromChild(slot);
                if (!string.IsNullOrEmpty(name)) return name;
            }
            catch { }

            return string.Empty;
        }
        
        private void LogItemDiagnostics(Item item)
        {
            // Diagnostics method kept for future debugging, currently not called
        }

        private string TryGetItemNameViaProperties(GUI_InventorySlot slot)
        {
            // The Il2CppInterop wrapper exposes Il2Cpp fields as C# PROPERTIES.
            // Use GetProperties (not GetFields) on the properly-typed cast object.
            var type = slot.GetType();

            // Common property name patterns to try
            string[] dataPropertyCandidates = {
                "_slotData", "SlotData", "_data",
                "_model", "Model", "_slotModel",
                "_item", "Item", "CurrentItem",
                "_itemStack", "ItemStack",
                "_inventorySlot", "InventorySlot",
                "_storableItem", "StorableItem"
            };

            foreach (var propName in dataPropertyCandidates)
            {
                var prop = type.GetProperty(propName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop == null) continue;

                var value = prop.GetValue(slot);
                if (value == null) continue;

                // Try to get name from the returned object
                var name = ExtractNameFromObject(value);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }

            return string.Empty;
        }

        private string ExtractNameFromObject(object obj)
        {
            if (obj == null) return string.Empty;
            var type = obj.GetType();

            // Try common "item name" property patterns
            string[] nameCandidates = {
                "_displayName", "DisplayName", "displayName",
                "_itemName", "ItemName", "itemName",
                "name", "Name", "_name",
                "_localizedName", "LocalizedName",
                "_readableId", "ReadableId",
                "Guid", "_guid"
            };

            foreach (var candidate in nameCandidates)
            {
                var prop = type.GetProperty(candidate,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null)
                {
                    var val = prop.GetValue(obj);
                    if (val != null && !string.IsNullOrEmpty(val.ToString()))
                        return val.ToString()!;
                }
                var field = type.GetField(candidate,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    var val = field.GetValue(obj);
                    if (val != null && !string.IsNullOrEmpty(val.ToString()))
                        return val.ToString()!;
                }
            }

            // Try nested: item → name
            string[] itemProps = { "_item", "Item", "_itemAsset", "ItemAsset" };
            foreach (var ip in itemProps)
            {
                var prop = type.GetProperty(ip,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop == null) continue;
                var itemObj = prop.GetValue(obj);
                if (itemObj == null) continue;
                var name = ExtractNameFromObject(itemObj);
                if (!string.IsNullOrEmpty(name)) return name;
            }

            return string.Empty;
        }

        private string TryGetItemNameNative(GUI_InventorySlot slot)
        {
            // Use Il2Cpp native API to iterate actual il2cpp fields (not C# wrapper fields)
            var classPtr = Il2CppClassPointerStore<GUI_InventorySlot>.NativeClassPtr;
            if (classPtr == IntPtr.Zero) return string.Empty;

            string[] fieldCandidates = {
                "_slotData", "_model", "_slotModel", "_item", "_itemStack",
                "_data", "_inventorySlot", "_currentItem"
            };

            foreach (var candidateName in fieldCandidates)
            {
                IntPtr fieldPtr = IL2CPP.il2cpp_class_get_field_from_name(classPtr, candidateName);
                if (fieldPtr == IntPtr.Zero) continue;

                unsafe
                {
                    IntPtr valuePtr = IntPtr.Zero;
                    IL2CPP.il2cpp_field_get_value(slot.Pointer, fieldPtr, &valuePtr);

                    if (valuePtr == IntPtr.Zero) continue;

                    // Try to get 'name' property from this object (it's a UnityEngine.Object subclass)
                    // Wrap as Il2CppObjectBase and use .ToString()
                    try
                    {
                        var wrapped = new Il2CppSystem.Object(valuePtr);
                        var str = wrapped.ToString();
                        if (!string.IsNullOrEmpty(str) && str != "null")
                        {
                            return str;
                        }
                    }
                    catch { }
                }
            }

            return string.Empty;
        }

        private string TryGetSpriteNameFromChild(GUI_InventorySlot slot)
        {
            // The slot has a child named "Image" containing the item icon sprite
            var imageTransform = slot.transform.Find("Image");
            if (imageTransform == null) return string.Empty;

            var img = imageTransform.GetComponent<Image>();
            if (img?.sprite == null) return string.Empty;

            // Sprite name is the internal asset name (e.g., "Icon_Sword_Iron_01")
            return img.sprite.name;
        }

        // ─────────────────────────────────────────────────────────────────
        // DIAGNOSTICS: Run once to log all available fields/properties
        // ─────────────────────────────────────────────────────────────────
        private void RunMainInventoryDiagnostics(GameObject mainInventoryPanel)
        {
            try
            {
                var components = mainInventoryPanel.GetComponents<Component>();

                Component mainInvComp = null;
                foreach (var comp in components)
                {
                    string typeName = comp.GetIl2CppType().FullName;
                    if (typeName.Contains("GUI_GameMenu_MainInventoryPanel"))
                    {
                        mainInvComp = comp;
                        break;
                    }
                }

                if (mainInvComp != null)
                {
                    // Component found, category buttons will be set up below
                }
                
                Transform inventoryTransform = mainInventoryPanel.transform.Find("Inventory");
                if (inventoryTransform != null)
                {
                    Transform toggleCategoryTransform = inventoryTransform.Find("ToggleCategorySlots_Horizontal");
                    if (toggleCategoryTransform != null)
                    {
                        GameObject toggleCategoryGO = toggleCategoryTransform.gameObject;
                        
                        Transform allButtonTransform = toggleCategoryGO.transform.Find("CategorySlots_All");
                        if (allButtonTransform != null)
                        {
                            _allCategoryButton = allButtonTransform.GetComponent<UnityEngine.UI.Button>();
                        }
                        
                        _categoryButtons.Clear();
                        string[] categoryButtonNames = {
                            "CategorySlots_All",
                            "CategorySlots_Weapon", 
                            "CategorySlots_Armor",
                            "CategorySlots_Consumable",
                            "CategorySlots_Traps",
                            "CategorySlots_Miscs",
                            "CategorySlots_KeysAndQuestItems",
                            "CategorySlots_SaleItem"
                        };
                        
                        foreach (var btnName in categoryButtonNames)
                        {
                            Transform btnTransform = toggleCategoryGO.transform.Find(btnName);
                            if (btnTransform != null)
                            {
                                var btn = btnTransform.GetComponent<UnityEngine.UI.Button>();
                                if (btn != null)
                                {
                                    _categoryButtons.Add(btn);
                                    bool isAllButton = btnName == "CategorySlots_All";
                                    
                                    // Create a UnityAction delegate that we can track and remove later
                                    UnityEngine.Events.UnityAction listener = new Action(() => OnCategoryButtonClicked(isAllButton));
                                    _categoryButtonListeners[btn] = listener;
                                    btn.onClick.AddListener(listener);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void RestoreOriginalView()
        {
            if (_currentInventoryWindow == null) return;

            var slots = _currentInventoryWindow.gameObject
                .GetComponentsInChildren<GUI_InventorySlot>(true);

            foreach (var slot in slots)
            {
                if (slot == null) continue;
                slot.gameObject.SetActive(true);
            }

            _isSearchActive = false;
        }

        private void CleanupSearch()
        {
            try
            {
                RestoreOriginalView();
                
                // Remove only our listeners from category buttons, not the game's original listeners
                foreach (var kvp in _categoryButtonListeners)
                {
                    var btn = kvp.Key;
                    var listener = kvp.Value;
                    if (btn != null && listener != null)
                    {
                        btn.onClick.RemoveListener(listener);
                    }
                }
                _categoryButtonListeners.Clear();
                _categoryButtons.Clear();
                _allCategoryButton = null;
                
                if (_searchInputObject != null)
                {
                    GameObject.Destroy(_searchInputObject);
                    _searchInputObject = null;
                    _searchInput = null;
                }
                _currentInventoryWindow = null;
                _currentSearchText = string.Empty;
                _isSearchActive = false;
                _diagnosticsDone = false;
                _mainInventoryDiagnosticsDone = false; // Reset so listeners are re-added on next load
            }
            catch (Exception ex) { MelonLogger.Error($"[Inventory Search] Cleanup error: {ex.Message}"); }
        }

        public override void OnDeinitializeMelon()
        {
            CleanupSearch();
        }
    }
}
