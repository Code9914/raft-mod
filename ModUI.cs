using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RaftMod
{
    public class ModUI
    {
        public static ModUI Instance { get; set; }

        private bool _showMenu;
        private int _currentTab;
        private bool _gameReady;
        private float _readyTimer;
        private Rect _windowRect = new Rect(50, 50, 540, 710);

        private readonly string[] _tabs = { "Player", "Items", "World", "Raft", "Extra", "Controls" };

        private GUISkin _skin;
        private bool _skinInit;
        private Texture2D _texAccent;
        private Texture2D _texToggleTrackOn;
        private Texture2D _texToggleTrackOff;
        private Texture2D _texThumb;
        private Texture2D _texSectionLine;
        private Texture2D _texHover;
        private List<Item_Base> _itemCache;
        private float _itemCacheTimer;
        private GUIStyle _rowEven;
        private GUIStyle _rowOdd;
        private GUIStyle _sectionStyle;
        private GUIStyle _tabLabelStyle;

        private class KeyBind
        {
            public string Id;
            public string Label;
            public string Tooltip;
            public KeyCode DefaultKey;
            public KeyCode CurrentKey;
            public Action Action;
        }

        private List<KeyBind> _keyBinds;
        private int _listeningIdx = -1;

        public PlayerFeatures Player { get; } = new PlayerFeatures();
        public ItemFeatures Item { get; } = new ItemFeatures();
        public WorldFeatures World { get; } = new WorldFeatures();
        public RaftFeatures Raft { get; } = new RaftFeatures();
        public ExtraFeatures Extra { get; } = new ExtraFeatures();

        private static readonly Color Accent = new Color(0.55f, 0.30f, 0.95f);
        private static readonly Color AccentHover = new Color(0.65f, 0.42f, 1.0f);
        private static readonly Color AccentDim = new Color(0.40f, 0.20f, 0.75f);
        private static readonly Color BgMain = new Color(0.05f, 0.02f, 0.08f);
        private static readonly Color BgTab = new Color(0.09f, 0.04f, 0.13f);
        private static readonly Color BgPanel = new Color(0.11f, 0.05f, 0.16f);
        private static readonly Color BgHover = new Color(0.18f, 0.08f, 0.26f);
        private static readonly Color BgElement = new Color(0.14f, 0.06f, 0.20f);
        private static readonly Color TextMain = new Color(0.92f, 0.88f, 0.96f);
        private static readonly Color TextDim = new Color(0.55f, 0.45f, 0.70f);
        private static readonly Color ToggleOff = new Color(0.20f, 0.10f, 0.32f);
        private static readonly Color ToggleOnColor = new Color(0.50f, 0.25f, 0.90f);
        private static readonly Color ThumbColor = new Color(0.95f, 0.92f, 1.0f);

        private void InitSkin()
        {
            if (_skinInit) return;
            _skinInit = true;

            _skin = ScriptableObject.CreateInstance<GUISkin>();
            _skin.font = GUI.skin.font;

            _skin.window = new GUIStyle(GUI.skin.window)
            {
                normal = {
                    background = MakeTexture(32, 32, BgMain),
                    textColor = TextMain
                },
                border = new RectOffset(8, 8, 28, 8),
                padding = new RectOffset(14, 14, 34, 14),
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            _skin.box = new GUIStyle(GUI.skin.box)
            {
                normal = {
                    background = MakeTexture(1, 1, BgPanel),
                    textColor = TextDim
                },
                border = new RectOffset(6, 6, 6, 6),
                padding = new RectOffset(10, 8, 7, 7),
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            _skin.label = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = TextMain },
                fontSize = 12,
                padding = new RectOffset(4, 2, 3, 3),
                wordWrap = false
            };

            _skin.toggle = new GUIStyle(GUI.skin.toggle)
            {
                normal = { textColor = TextMain, background = MakeTexture(1, 1, Color.clear) },
                onNormal = { textColor = TextMain, background = MakeTexture(1, 1, Color.clear) },
                hover = { textColor = Color.white, background = MakeTexture(1, 1, Color.clear) },
                onHover = { textColor = Color.white, background = MakeTexture(1, 1, Color.clear) },
                padding = new RectOffset(4, 4, 3, 3),
                fontSize = 12,
                border = new RectOffset(0, 0, 0, 0),
                overflow = new RectOffset(0, 0, 0, 0),
                fontStyle = FontStyle.Normal
            };

            _skin.button = new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = TextMain, background = MakeRoundedRect(32, 32, AccentDim, 4) },
                hover = { textColor = Color.white, background = MakeRoundedRect(32, 32, Accent, 4) },
                active = { textColor = TextDim, background = MakeRoundedRect(32, 32, AccentDim * 0.6f, 4) },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(10, 10, 5, 5),
                border = new RectOffset(4, 4, 4, 4),
                margin = new RectOffset(4, 4, 2, 2),
                alignment = TextAnchor.MiddleCenter
            };

            _skin.horizontalSlider = new GUIStyle(GUI.skin.horizontalSlider)
            {
                normal = { background = MakeRoundedRect(16, 6, ToggleOff, 3) },
                border = new RectOffset(3, 3, 3, 3),
                margin = new RectOffset(0, 0, 6, 6),
                fixedHeight = 6
            };
            _skin.horizontalSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                normal = { background = MakeCircle(8, Accent) },
                hover = { background = MakeCircle(8, AccentHover) },
                border = new RectOffset(4, 4, 4, 4),
                fixedWidth = 14,
                fixedHeight = 14
            };

            _skin.verticalScrollbar = new GUIStyle(GUI.skin.verticalScrollbar)
            {
                normal = { background = MakeTexture(2, 1, Color.clear) },
                border = new RectOffset(1, 1, 1, 1),
                fixedWidth = 8
            };
            _skin.verticalScrollbarThumb = new GUIStyle(GUI.skin.verticalScrollbarThumb)
            {
                normal = { background = MakeRoundedRect(6, 16, new Color(0.35f, 0.20f, 0.50f), 3) },
                hover = { background = MakeRoundedRect(6, 16, AccentDim, 3) },
                border = new RectOffset(3, 3, 3, 3),
                fixedWidth = 6
            };

            _sectionStyle = new GUIStyle(_skin.label) { fontSize = 10, fontStyle = FontStyle.Bold };
            _tabLabelStyle = new GUIStyle(_skin.label) { alignment = TextAnchor.MiddleLeft };

            GUI.skin = _skin;
            _texAccent = MakeTexture(1, 1, Accent);
            _texToggleTrackOn = MakeRoundedRect(36, 18, ToggleOnColor, 9);
            _texToggleTrackOff = MakeRoundedRect(36, 18, ToggleOff, 9);
            _texThumb = MakeCircle(7, ThumbColor);
            _texSectionLine = MakeTexture(2, 1, Accent);
            _texHover = MakeTexture(1, 1, new Color(0.14f, 0.06f, 0.22f));

            _rowEven = new GUIStyle
            {
                normal = { background = MakeTexture(1, 1, new Color(0.08f, 0.03f, 0.12f)) },
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(8, 4, 4, 4),
                border = new RectOffset(1, 1, 1, 1)
            };
            _rowOdd = new GUIStyle
            {
                normal = { background = MakeTexture(1, 1, new Color(0.05f, 0.02f, 0.08f)) },
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(8, 4, 4, 4),
                border = new RectOffset(1, 1, 1, 1)
            };
        }

        private static Texture2D MakeTexture(int w, int h, Color c)
        {
            var t = new Texture2D(w, h);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    t.SetPixel(x, y, c);
            t.Apply();
            return t;
        }

        private static Texture2D MakeGradTexture(int w, int h, Color top, Color bottom)
        {
            var t = new Texture2D(w, h);
            for (int y = 0; y < h; y++)
            {
                var c = Color.Lerp(top, bottom, y / (float)(h - 1));
                for (int x = 0; x < w; x++)
                    t.SetPixel(x, y, c);
            }
            t.Apply();
            return t;
        }

        private static Texture2D MakeBorderedTexture(int w, int h, Color bg, Color borderColor)
        {
            var t = new Texture2D(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    t.SetPixel(x, y, x < 3 ? borderColor : bg);
            t.Apply();
            return t;
        }

        private static Texture2D MakeRoundedRect(int w, int h, Color color, float radius)
        {
            var t = new Texture2D(w, h, TextureFormat.ARGB32, false);
            float r2 = radius * radius;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool topLeft = x < radius && y < radius && (x - radius) * (x - radius) + (y - radius) * (y - radius) > r2;
                    bool topRight = x > w - 1 - radius && y < radius && (x - (w - 1 - radius)) * (x - (w - 1 - radius)) + (y - radius) * (y - radius) > r2;
                    bool bottomLeft = x < radius && y > h - 1 - radius && (x - radius) * (x - radius) + (y - (h - 1 - radius)) * (y - (h - 1 - radius)) > r2;
                    bool bottomRight = x > w - 1 - radius && y > h - 1 - radius && (x - (w - 1 - radius)) * (x - (w - 1 - radius)) + (y - (h - 1 - radius)) * (y - (h - 1 - radius)) > r2;
                    if (topLeft || topRight || bottomLeft || bottomRight)
                        t.SetPixel(x, y, Color.clear);
                    else
                        t.SetPixel(x, y, color);
                }
            }
            t.Apply();
            return t;
        }

        private static Texture2D MakeCircle(int r, Color color)
        {
            int d = r * 2;
            var t = new Texture2D(d, d, TextureFormat.ARGB32, false);
            float r2 = r * r;
            float cx = r - 0.5f;
            float cy = r - 0.5f;
            for (int y = 0; y < d; y++)
            {
                for (int x = 0; x < d; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    t.SetPixel(x, y, dx * dx + dy * dy <= r2 ? color : Color.clear);
                }
            }
            t.Apply();
            return t;
        }

        public void Update()
        {
            var player = ComponentManager<Network_Player>.Value;

            if (!_gameReady)
            {
                if (player != null)
                {
                    _readyTimer += Time.deltaTime;
                    if (_readyTimer > 5f)
                    {
                        _gameReady = true;
                        Extra.MenuOpen = false;
                        InitKeyBinds();
                        Plugin.Log.LogInfo("Mod UI ready!");
                    }
                }
                else
                {
                    _readyTimer = 0f;
                }
                return;
            }

            if (player == null)
            {
                _gameReady = false;
                _skinInit = false;
                _readyTimer = 0f;
                return;
            }

            if (_listeningIdx >= 0)
            {
                if (Input.anyKeyDown)
                {
                    foreach (KeyCode k in System.Enum.GetValues(typeof(KeyCode)))
                    {
                        if (Input.GetKeyDown(k))
                        {
                            if (k == KeyCode.Escape)
                            {
                                _listeningIdx = -1;
                                break;
                            }
                            if (k == KeyCode.Delete || k == KeyCode.Backspace)
                            {
                                if (_keyBinds[_listeningIdx].Id == "menu_toggle")
                                {
                                    _listeningIdx = -1;
                                    break;
                                }
                                _keyBinds[_listeningIdx].CurrentKey = KeyCode.None;
                                PlayerPrefs.SetInt("kb_" + _keyBinds[_listeningIdx].Id, (int)KeyCode.None);
                                PlayerPrefs.Save();
                                _listeningIdx = -1;
                                break;
                            }
                            _keyBinds[_listeningIdx].CurrentKey = k;
                            PlayerPrefs.SetInt("kb_" + _keyBinds[_listeningIdx].Id, (int)k);
                            PlayerPrefs.Save();
                            _listeningIdx = -1;
                            break;
                        }
                    }
                }
                return;
            }

            try
            {
                for (int i = 0; i < _keyBinds.Count; i++)
                {
                    if (_keyBinds[i].CurrentKey != KeyCode.None && Input.GetKeyDown(_keyBinds[i].CurrentKey))
                        _keyBinds[i].Action();
                }
            }
            catch { }

            try { Player.Update(); } catch (Exception ex) { Plugin.Log.LogError($"Player: {ex.Message}"); }
            try { Item.Update(); } catch (Exception ex) { Plugin.Log.LogError($"Item: {ex.Message}"); }
            try { World.Update(); } catch (Exception ex) { Plugin.Log.LogError($"World: {ex.Message}"); }
            try { Raft.Update(); } catch (Exception ex) { Plugin.Log.LogError($"Raft: {ex.Message}"); }
            try { Extra.Update(); } catch (Exception ex) { Plugin.Log.LogError($"Extra: {ex.Message}"); }
        }

        private void InitKeyBinds()
        {
            if (_keyBinds != null) return;

            _keyBinds = new List<KeyBind>();

            void Add(string id, string label, string tooltip, KeyCode defaultKey, Action action)
            {
                var saved = (KeyCode)PlayerPrefs.GetInt("kb_" + id, (int)defaultKey);
                _keyBinds.Add(new KeyBind { Id = id, Label = label, Tooltip = tooltip, DefaultKey = defaultKey, CurrentKey = saved, Action = action });
            }

            Add("menu_toggle", "Toggle Menu", "Open/close the mod menu", KeyCode.F5, () => { _showMenu = !_showMenu; Extra.MenuOpen = _showMenu; });
            Add("noclip", "Noclip", "Toggle noclip/fly mode", KeyCode.None, () => { Player.NoClip = !Player.NoClip; });
            Add("godmode", "God Mode", "Toggle god mode", KeyCode.None, () => { Player.GodMode = !Player.GodMode; });
            Add("coordinates", "Show Coordinates", "Toggle coordinates HUD", KeyCode.None, () => { Extra.ShowCoordinates = !Extra.ShowCoordinates; });
            Add("thirdperson", "Third Person", "Toggle third person camera", KeyCode.None, () => { Extra.ThirdPerson = !Extra.ThirdPerson; });
            Add("infinite_items", "Infinite Items", "Toggle infinite items", KeyCode.None, () => { Item.InfiniteItems = !Item.InfiniteItems; });
            Add("creative", "Creative Mode", "Toggle creative mode", KeyCode.None, () => { Extra.SetCreativeMode(!Extra.IsCreativeMode()); });
        }

        public void OnGUI()
        {
            if (!_gameReady) return;

            if (Extra.ShowCoordinates)
                DrawCoordinatesOverlay();

            if (!_showMenu) return;

            InitSkin();
            _windowRect = GUI.Window(0, _windowRect, DrawWindow, "Raft Mod Menu");
        }

        private void DrawWindow(int id)
        {
            GUI.DrawTexture(new Rect(0, 0, _windowRect.width, _windowRect.height), _skin.window.normal.background);

            var headerRect = new Rect(0, 0, _windowRect.width, 28);
            GUI.DrawTexture(new Rect(14, 26, 40, 2), _texAccent);

            var origC = GUI.contentColor;
            GUI.contentColor = TextDim;
            GUI.Label(new Rect(14, 5, 200, 20), "Raft Mod Menu");
            GUI.contentColor = Accent;
            GUI.Label(new Rect(14, 17, 80, 12), "v1.6.0", new GUIStyle(_skin.label) { fontSize = 9 });
            GUI.contentColor = origC;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(85));
            DrawSideTabs();
            GUILayout.EndVertical();
            GUI.DrawTexture(new Rect(85, 28, 1, _windowRect.height - 28), _texAccent);
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            DrawCurrentTab();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUI.DragWindow(headerRect);
        }

        private void DrawSideTabs()
        {
            GUILayout.Space(8);
            for (int i = 0; i < _tabs.Length; i++)
            {
                var isActive = _currentTab == i;
                var r = GUILayoutUtility.GetRect(new GUIContent(_tabs[i]), _skin.button, GUILayout.ExpandWidth(true), GUILayout.Height(30));

                var bgRect = new Rect(r.x, r.y, 85, 30);
                if (isActive)
                {
                    GUI.DrawTexture(new Rect(0, r.y, 3, 30), _texAccent);
                    GUI.DrawTexture(bgRect, MakeTexture(1, 1, BgPanel));
                }
                else if (bgRect.Contains(Event.current.mousePosition))
                    GUI.DrawTexture(bgRect, _texHover);

                var origC = GUI.contentColor;
                GUI.contentColor = isActive ? TextMain : TextDim;
                _tabLabelStyle.fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal;
                GUI.Label(new Rect(r.x + 8, r.y, r.width - 8, 30), _tabs[i], _tabLabelStyle);
                GUI.contentColor = origC;

                if (Event.current.type == EventType.MouseDown && bgRect.Contains(Event.current.mousePosition) && !isActive)
                {
                    _currentTab = i;
                    Event.current.Use();
                }
            }
            GUILayout.FlexibleSpace();
        }

        private void DrawCurrentTab()
        {
            switch (_currentTab)
            {
                case 0: DrawPlayerTab(); break;
                case 1: DrawItemTab(); break;
                case 2: DrawWorldTab(); break;
                case 3: DrawRaftTab(); break;
                case 4: DrawExtraTab(); break;
                case 5: DrawControlsTab(); break;
            }
        }

        // ──────────────── PLAYER TAB ────────────────
        private Vector2 _pscroll;
        private void DrawPlayerTab()
        {
            _pscroll = GUILayout.BeginScrollView(_pscroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Section("Health");
            Player.GodMode = Toggle(new GUIContent("God Mode", "Invincible, unlimited health and bonus HP"), Player.GodMode);
            if (Button("Heal Full", "Restore health and bonus HP to maximum")) Player.HealPlayer();
            LabelValue("Health", $"{Player.GetHealth():F0}");
            LabelValue("Bonus HP", $"{Player.GetBonusHealth():F0}");

            Section("Vitals");
            Player.InfiniteOxygen = Toggle(new GUIContent("Infinite Oxygen", "Oxygen never depletes"), Player.InfiniteOxygen);
            Player.InfiniteHunger = Toggle(new GUIContent("Infinite Hunger/Thirst", "Hunger and thirst stay full"), Player.InfiniteHunger);
            Player.NoHungerLoss = Toggle(new GUIContent("No Hunger/Thirst Loss", "Disables hunger and thirst drain"), Player.NoHungerLoss);

            Section("Movement");
            Player.NoClip = Toggle(new GUIContent("Noclip / Fly Mode", "Free flight camera, no collision"), Player.NoClip);
            InlineSlider("Speed", ref Player.MoveSpeed, 0.5f, 20f, "x");
            InlineSlider("Jump", ref Player.JumpMultiplier, 0.5f, 10f, "x");
            InlineSlider("Swim", ref Player.SwimMultiplier, 0.5f, 10f, "x");
            InlineSlider("Gravity", ref Player.Gravity, 5f, 50f, "");

            Section("Combat");
            Player.InfDurability = Toggle(new GUIContent("Infinite Durability", "Tools and weapons never lose durability"), Player.InfDurability);
            Player.NoFallDamage = Toggle(new GUIContent("No Fall Damage", "Disable all fall damage"), Player.NoFallDamage);
            InlineSlider("Damage Multiplier", ref Player.DamageMultiplier, 0f, 20f, "x");

            Section("Convenience");
            Player.AutoPickup = Toggle(new GUIContent("Auto Pickup Items", "Automatically pick up nearby items"), Player.AutoPickup);
            InlineSlider("Field of View", ref Player.FOV, 40f, 120f, "");

            GUILayout.EndScrollView();
        }

        // ──────────────── ITEMS TAB ────────────────
        private Vector2 _iscroll;
        private string _itemSearch = "";
        private void DrawItemTab()
        {
            _iscroll = GUILayout.BeginScrollView(_iscroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Section("Inventory");
            Item.InfiniteItems = Toggle(new GUIContent("Infinite Items", "Keeps item stacks at 10"), Item.InfiniteItems);
            if (Button("Give All Items (1 each)", "Adds one of every item to your inventory")) Item.GiveAllItems = true;
            if (DangerButton("Clear Inventory", "Removes all items from your inventory")) Item.ClearInventory();

            Section("Research & Crafting");
            if (Button("Unlock All Research", "Learn all crafting recipes instantly")) Item.UnlockAll = true;
            if (Button("Unlock All Blueprints", "Unlock all blueprints (may need save reload)")) Item.UnlockBlueprints = true;

            Section("Item Spawner");

            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            var orig = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.15f, 0.05f, 0.25f);
            _itemSearch = GUILayout.TextField(_itemSearch, GUILayout.ExpandWidth(true), GUILayout.Height(22));
            GUI.backgroundColor = orig;
            if (GUILayout.Button("x", GUILayout.Width(24), GUILayout.Height(22)))
                _itemSearch = "";
            GUILayout.Space(4);
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            if (_itemCache == null || Time.time > _itemCacheTimer + 30f)
            {
                _itemCache = ItemManager.GetAllItems();
                _itemCacheTimer = Time.time;
            }

            var filtered = string.IsNullOrEmpty(_itemSearch.Trim())
                ? _itemCache
                : _itemCache.Where(i =>
                  {
                      try { return i.UniqueName.IndexOf(_itemSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   i.settings_Inventory.DisplayName.IndexOf(_itemSearch, StringComparison.OrdinalIgnoreCase) >= 0; }
                      catch { return false; }
                  }).ToList();

            for (int idx = 0; idx < filtered.Count; idx++)
            {
                var item = filtered[idx];
                var sprite = item.settings_Inventory?.Sprite;
                var name = item.settings_Inventory?.DisplayName ?? item.UniqueName;

                var rowStyle = idx % 2 == 0 ? _rowEven : _rowOdd;
                GUILayout.BeginHorizontal(rowStyle ?? GUIStyle.none, GUILayout.Height(28));

                var iconRect = GUILayoutUtility.GetRect(22, 22, GUILayout.Width(26));
                if (sprite != null)
                    DrawSprite(iconRect, sprite);

                GUILayout.Space(6);
                GUILayout.Label(name, GUILayout.Width(140));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("+1", GUILayout.Width(32), GUILayout.Height(20)))
                    Item.SpawnItem(item.UniqueName, 1);
                if (GUILayout.Button("+10", GUILayout.Width(34), GUILayout.Height(20)))
                    Item.SpawnItem(item.UniqueName, 10);

                GUILayout.Space(2);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        // ──────────────── WORLD TAB ────────────────
        private Vector2 _wscroll;
        private void DrawWorldTab()
        {
            _wscroll = GUILayout.BeginScrollView(_wscroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Section("Time");
            World.AlwaysDay = Toggle(new GUIContent("Always Day", "Forces time to noon"), World.AlwaysDay);
            LabelValue("Current Time", $"{World.GetCurrentHour():F1}h");
            InlineSlider("Target", ref World.TargetHour, 0f, 24f, "h");
            if (Button("Set Time", $"Set time to {World.TargetHour:F0}:00")) World.SetHour(World.TargetHour);
            InlineSlider("Speed", ref World.TimeSpeed, 0f, 10f, "x");

            Section("Weather");
            GUILayout.BeginHorizontal();
            if (Button("Clear", "Clear weather")) World.SetWeather(0);
            if (Button("Rain", "Rainy weather")) World.SetWeather(1);
            if (Button("Storm", "Heavy storm")) World.SetWeather(2);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (Button("Fog", "Foggy weather")) World.SetWeather(3);
            if (Button("Snow", "Snowy weather")) World.SetWeather(4);
            if (Button("Calm", "Calm seas")) World.SetWeather(5);
            GUILayout.EndHorizontal();

            Section("Entities");
            World.NoEnemies = Toggle(new GUIContent("No Enemies", "Auto-kills all enemies every second"), World.NoEnemies);
            World.NoShark = Toggle(new GUIContent("Disable Shark", "Auto-kills Bruce the shark"), World.NoShark);
            if (DangerButton("Kill All Enemies", "Instantly kill all enemies in the world")) World.KillAllEnemies();

            Section("Teleport");
            if (Button("Teleport to Raft", "Teleport yourself to the raft")) World.TeleportToRaft();
            if (Button("Teleport to Crosshair", "Teleport to where you are looking")) World.TeleportToCamera();

            Section("Fishing");
            World.InstantFish = Toggle(new GUIContent("Instant Fish", "Fish bites immediately when you cast"), World.InstantFish);
            World.AutoCatch = Toggle(new GUIContent("Auto Catch", "Automatically reel in fish when hooked"), World.AutoCatch);

            GUILayout.EndScrollView();
        }

        // ──────────────── RAFT TAB ────────────────
        private Vector2 _rscroll;
        private void DrawRaftTab()
        {
            _rscroll = GUILayout.BeginScrollView(_rscroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Section("Raft Settings");
            InlineSlider("Max Speed", ref Raft.MaxSpeed, 1f, 30f, "");
            InlineSlider("Acceleration", ref Raft.Acceleration, 0.5f, 20f, "");

            Section("Raft Actions");
            if (Button("Teleport Raft to Player", "Brings the raft to your position")) Raft.TeleportToPlayer();
            if (Button("Teleport Player to Raft", "Sends you back to the raft")) World.TeleportToRaft();

            Section("Engine");
            Raft.InfiniteFuel = Toggle(new GUIContent("Infinite Fuel", "Engines never run out of fuel"), Raft.InfiniteFuel);
            Raft.AnchorAll = Toggle(new GUIContent("Auto Anchor", "Keeps all anchors deployed"), Raft.AnchorAll);

            Section("Protection");
            Raft.AutoRepair = Toggle(new GUIContent("Auto Repair", "Automatically repair raft blocks when damaged"), Raft.AutoRepair);

            GUILayout.EndScrollView();
        }

        // ──────────────── EXTRA TAB ────────────────
        private Vector2 _escroll;
        private int _selectedLandmark;
        private void DrawExtraTab()
        {
            _escroll = GUILayout.BeginScrollView(_escroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Section("Game Mode");
            var wasCreative = Extra.IsCreativeMode();
            var nowCreative = Toggle(new GUIContent("Creative Mode", "Disable hunger, instant build, no costs"), wasCreative);
            if (nowCreative != wasCreative) Extra.SetCreativeMode(nowCreative);
            InlineSlider("Game Speed", ref Extra.GameSpeed, 0.1f, 10f, "x");

            Section("Progression");
            if (Button("Unlock All Achievements", "Unlock every Steam achievement")) Extra.UnlockAllAchievements();
            if (Button("Unlock All Notes", "Discover all story notes")) Extra.UnlockAllNotes();
            if (Button("Unlock All Frequencies", "Activate all radio frequencies")) Extra.UnlockAllFrequencies();
            if (Button("Finish All Quests", "Complete all story quests instantly")) Extra.FinishAllQuests();

            if (Button("Unlock All Characters", "Unlock every playable character")) Extra.UnlockAllCharacters();

            Section("Plants");
            if (Button("Instant Grow All Plants", "Skip growth time for all planted crops")) Extra.InstantGrowPlants();
            if (Button("Harvest All Plants", "Collect all fully-grown crops")) Extra.HarvestAllPlants();

            Section("Utilities");
            Extra.ShowCoordinates = Toggle(new GUIContent("Show Coordinates", "Display position HUD on screen"), Extra.ShowCoordinates);
            Extra.ThirdPerson = Toggle(new GUIContent("Third Person", "Move camera behind the player"), Extra.ThirdPerson);
            Extra.InfiniteBattery = Toggle(new GUIContent("Infinite Battery", "All batteries stay charged"), Extra.InfiniteBattery);

            Section("Teleport to Landmark");
            var lmCount = Extra.GetLandmarkCount();
            if (lmCount > 0)
            {
                var names = Extra.GetLandmarkNames();
                int sel = Mathf.Clamp(_selectedLandmark, 0, lmCount - 1);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<", _skin.button, GUILayout.Width(28), GUILayout.Height(24)))
                    sel = (sel - 1 + lmCount) % lmCount;
                var lmName = names.Length > sel ? names[sel] : "?";
                GUILayout.Label(lmName, _skin.label);
                if (GUILayout.Button(">", _skin.button, GUILayout.Width(28), GUILayout.Height(24)))
                    sel = (sel + 1) % lmCount;
                GUILayout.EndHorizontal();
                _selectedLandmark = sel;
                if (Button("Teleport", $"Teleport to {lmName}")) Extra.TeleportToLandmark(sel);
            }
            else
            {
                GUILayout.Label("No landmarks found nearby");
            }

            GUILayout.EndScrollView();
        }

        private Vector2 _cscroll;
        private void DrawControlsTab()
        {
            _cscroll = GUILayout.BeginScrollView(_cscroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Section("Key Bindings");
            GUILayout.Space(2);
            var origC = GUI.contentColor;
            GUI.contentColor = TextDim;
            GUILayout.Label("Click a key to rebind. ESC=cancel, DEL=clear.");
            GUI.contentColor = origC;
            GUILayout.Space(4);

            for (int idx = 0; idx < _keyBinds.Count; idx++)
            {
                var kb = _keyBinds[idx];
                var isListening = _listeningIdx == idx;
                var rowStyle = idx % 2 == 0 ? _rowEven : _rowOdd;

                GUILayout.BeginHorizontal(rowStyle ?? GUIStyle.none, GUILayout.Height(26));

                GUILayout.Space(8);
                var label = kb.Id == "menu_toggle" ? kb.Label + " *" : kb.Label;
                GUILayout.Label(new GUIContent(label, kb.Tooltip), GUILayout.Width(150));

                GUILayout.FlexibleSpace();

                var keyLabel = isListening ? "Press a key..." : (kb.CurrentKey == KeyCode.None ? "None" : kb.CurrentKey.ToString());
                var keyStyle = isListening ? _skin.label : _skin.button;
                var keyWidth = isListening ? 145 : 90;

                if (GUILayout.Button(keyLabel, keyStyle, GUILayout.Width(keyWidth), GUILayout.Height(22)))
                {
                    if (!isListening)
                        _listeningIdx = idx;
                }

                GUILayout.Space(8);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        private void DrawCoordinatesOverlay()
        {
            var orig = GUI.contentColor;
            GUI.contentColor = Accent;
            GUI.Label(new Rect(Screen.width - 180, 10, 170, 20), Extra.GetCoordinates());
            GUI.contentColor = orig;
        }

        private void Section(string title)
        {
            GUILayout.Space(6);
            var r = GUILayoutUtility.GetRect(new GUIContent(title), _sectionStyle, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(new Rect(r.x + 4, r.y + 1, 2, r.height - 2), _texAccent);
            var origC = GUI.contentColor;
            GUI.contentColor = TextDim;
            GUI.Label(new Rect(r.x + 10, r.y, r.width - 10, r.height), title, _sectionStyle);
            GUI.contentColor = origC;
            GUILayout.Space(2);
        }

        private bool Switch(GUIContent content, bool value)
        {
            var rect = GUILayoutUtility.GetRect(220, 24);

            float trackX = rect.x + 6;
            float trackY = rect.y + (rect.height - 18) * 0.5f;

            var trackRect = new Rect(trackX, trackY, 36, 18);
            var thumbRect = new Rect(value ? trackX + 20 : trackX + 2, trackY + 2, 14, 14);

            GUI.DrawTexture(trackRect, value ? _texToggleTrackOn : _texToggleTrackOff);
            GUI.DrawTexture(thumbRect, _texThumb);

            var origC = GUI.contentColor;
            GUI.contentColor = value ? TextMain : TextDim;
            var labelRect = new Rect(trackRect.xMax + 8, rect.y, rect.width - trackRect.width - 16, rect.height);
            GUI.Label(labelRect, content.text, _skin.toggle);
            GUI.contentColor = origC;

            if (!string.IsNullOrEmpty(content.tooltip))
                GUI.Label(rect, new GUIContent("", content.tooltip));

            var ev = Event.current;
            if (ev.type == EventType.MouseDown && rect.Contains(ev.mousePosition))
            {
                value = !value;
                ev.Use();
            }

            return value;
        }

        private bool Switch(string label, bool value, string tooltip = "")
        {
            return Switch(new GUIContent(label, tooltip), value);
        }

        private bool Toggle(GUIContent content, bool value)
        {
            return Switch(content, value);
        }

        private bool Toggle(string label, bool value)
        {
            return Switch(new GUIContent(label, ""), value);
        }

        private bool Button(string label, string tooltip = "")
        {
            bool clicked = false;
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            if (GUILayout.Button(new GUIContent(label, tooltip), _skin.button, GUILayout.Height(26), GUILayout.ExpandWidth(true)))
                clicked = true;
            GUILayout.EndHorizontal();
            return clicked;
        }

        private bool DangerButton(string label, string tooltip = "")
        {
            var orig = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.45f, 0.08f, 0.12f);
            bool clicked = false;
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            if (GUILayout.Button(new GUIContent(label, tooltip), _skin.button, GUILayout.Height(26), GUILayout.ExpandWidth(true)))
                clicked = true;
            GUILayout.EndHorizontal();
            GUI.backgroundColor = orig;
            return clicked;
        }

        private void InlineSlider(string label, ref float value, float min, float max, string suffix)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            GUILayout.Label(label, GUILayout.Width(70));
            value = GUILayout.HorizontalSlider(value, min, max, _skin.horizontalSlider, _skin.horizontalSliderThumb, GUILayout.Height(16));
            var origC = GUI.contentColor;
            GUI.contentColor = Accent;
            GUILayout.Label(value.ToString(suffix == "x" ? "F1" : "F0") + suffix, GUILayout.Width(40));
            GUI.contentColor = origC;
            GUILayout.Space(8);
            GUILayout.EndHorizontal();
        }

        private void LabelValue(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            GUILayout.Label(label + ": ", GUILayout.Width(70));
            var orig = GUI.contentColor;
            GUI.contentColor = Accent;
            GUILayout.Label(value);
            GUI.contentColor = orig;
            GUILayout.EndHorizontal();
        }

        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null) return;
            var tex = sprite.texture;
            if (tex == null) return;
            var r = sprite.textureRect;
            var uv = new Rect(
                r.x / tex.width,
                r.y / tex.height,
                r.width / tex.width,
                r.height / tex.height
            );
            GUI.DrawTextureWithTexCoords(rect, tex, uv);
        }
    }
}
