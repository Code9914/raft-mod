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
        private Rect _windowRect = new Rect(50, 50, 470, 600);

        private readonly string[] _tabs = { "Player", "Items", "World", "Raft", "Extra" };

        private GUISkin _skin;
        private bool _skinInit;
        private Texture2D _texAccent;
        private string _tooltipText = "";
        private List<Item_Base> _itemCache;
        private float _itemCacheTimer;

        public PlayerFeatures Player { get; } = new PlayerFeatures();
        public ItemFeatures Item { get; } = new ItemFeatures();
        public WorldFeatures World { get; } = new WorldFeatures();
        public RaftFeatures Raft { get; } = new RaftFeatures();
        public ExtraFeatures Extra { get; } = new ExtraFeatures();

        private static readonly Color Purple = new Color(0.6f, 0.35f, 1f);
        private static readonly Color PurpleDark = new Color(0.4f, 0.15f, 0.8f);
        private static readonly Color PurpleGlow = new Color(0.7f, 0.5f, 1f);
        private static readonly Color PurpleMuted = new Color(0.45f, 0.3f, 0.65f);
        private static readonly Color BlackBg = Color.black;
        private static readonly Color PanelBg = new Color(0.03f, 0.0f, 0.05f);
        private static readonly Color ElementBg = new Color(0.06f, 0.0f, 0.10f);
        private static readonly Color HoverBg = new Color(0.12f, 0.04f, 0.18f);
        private static readonly Color ToggleOn = new Color(0.5f, 0.25f, 0.9f);
        private static readonly Color ToggleOnHover = new Color(0.6f, 0.35f, 1f);

        private void InitSkin()
        {
            if (_skinInit) return;
            _skinInit = true;

            _skin = ScriptableObject.CreateInstance<GUISkin>();
            _skin.font = GUI.skin.font;

            _skin.window = new GUIStyle(GUI.skin.window)
            {
                normal = {
                    background = MakeTexture(32, 32, Color.black),
                    textColor = new Color(0.9f, 0.8f, 1f)
                },
                border = new RectOffset(8, 8, 28, 8),
                padding = new RectOffset(12, 12, 32, 12),
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };

            _skin.box = new GUIStyle(GUI.skin.box)
            {
                normal = {
                    background = MakeBorderedTexture(16, 24, new Color(0.04f, 0.0f, 0.07f), Purple),
                    textColor = new Color(0.9f, 0.85f, 1f)
                },
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(10, 6, 5, 5),
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            _skin.label = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = new Color(0.85f, 0.82f, 0.9f) },
                fontSize = 12,
                padding = new RectOffset(4, 2, 3, 3),
                wordWrap = false
            };

            _skin.toggle = new GUIStyle(GUI.skin.toggle)
            {
                normal = { textColor = new Color(0.9f, 0.85f, 0.95f), background = MakeTexture(1, 1, ElementBg) },
                onNormal = { textColor = Color.white, background = MakeTexture(1, 1, ToggleOn) },
                hover = { textColor = Color.white, background = MakeTexture(1, 1, HoverBg) },
                onHover = { textColor = Color.white, background = MakeTexture(1, 1, ToggleOnHover) },
                padding = new RectOffset(24, 6, 5, 5),
                fontSize = 12,
                border = new RectOffset(4, 4, 5, 5),
                overflow = new RectOffset(0, 0, 0, 0),
                fontStyle = FontStyle.Normal
            };

            _skin.button = new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = Color.white, background = MakeTexture(1, 1, new Color(0.35f, 0.15f, 0.6f)) },
                hover = { textColor = Color.white, background = MakeTexture(1, 1, new Color(0.5f, 0.25f, 0.85f)) },
                active = { textColor = new Color(0.8f, 0.7f, 1f), background = MakeTexture(1, 1, new Color(0.25f, 0.1f, 0.4f)) },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(8, 8, 6, 6),
                border = new RectOffset(4, 4, 4, 4),
                margin = new RectOffset(0, 0, 2, 2),
                alignment = TextAnchor.MiddleCenter
            };

            _skin.horizontalSlider = new GUIStyle(GUI.skin.horizontalSlider)
            {
                normal = { background = MakeTexture(1, 1, new Color(0.02f, 0.0f, 0.04f)) },
                border = new RectOffset(4, 4, 4, 4),
                margin = new RectOffset(0, 0, 4, 4)
            };
            _skin.horizontalSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                normal = { background = MakeTexture(8, 16, Purple) },
                hover = { background = MakeTexture(8, 16, PurpleGlow) },
                border = new RectOffset(4, 4, 6, 6)
            };

            _skin.verticalScrollbar = new GUIStyle(GUI.skin.verticalScrollbar)
            {
                normal = { background = MakeTexture(2, 1, Color.black) },
                border = new RectOffset(1, 1, 1, 1),
                fixedWidth = 10
            };
            _skin.verticalScrollbarThumb = new GUIStyle(GUI.skin.verticalScrollbarThumb)
            {
                normal = { background = MakeTexture(8, 8, new Color(0.25f, 0.12f, 0.4f)) },
                hover = { background = MakeTexture(8, 8, new Color(0.4f, 0.2f, 0.6f)) },
                border = new RectOffset(3, 3, 3, 3),
                fixedWidth = 8
            };

            GUI.skin = _skin;
            _texAccent = MakeTexture(1, 1, Purple);
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

            try
            {
                if (Input.GetKeyDown(KeyCode.F5))
                {
                    _showMenu = !_showMenu;
                    Extra.MenuOpen = _showMenu;
                }
            }
            catch { }

            try { Player.Update(); } catch (Exception ex) { Plugin.Log.LogError($"Player: {ex.Message}"); }
            try { Item.Update(); } catch (Exception ex) { Plugin.Log.LogError($"Item: {ex.Message}"); }
            try { World.Update(); } catch (Exception ex) { Plugin.Log.LogError($"World: {ex.Message}"); }
            try { Raft.Update(); } catch (Exception ex) { Plugin.Log.LogError($"Raft: {ex.Message}"); }
            try { Extra.Update(); } catch (Exception ex) { Plugin.Log.LogError($"Extra: {ex.Message}"); }
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

        private Texture2D _bgBlack;

        private void DrawWindow(int id)
        {
            if (_bgBlack == null) _bgBlack = MakeTexture(2, 2, Color.black);
            GUI.DrawTexture(new Rect(0, 0, _windowRect.width, _windowRect.height), _bgBlack);

            GUILayout.BeginVertical();

            if (_texAccent != null)
                GUI.DrawTexture(new Rect(12, 30, _windowRect.width - 24, 2), _texAccent);

            if (_texAccent != null)
            {
                GUI.DrawTexture(new Rect(12, 30, _windowRect.width - 24, 1), _texAccent);
                GUI.DrawTexture(new Rect(12, 33, (_windowRect.width - 24) * 0.3f, 1), _texAccent);
            }

            DrawTabBar();
            DrawCurrentTab();

            GUILayout.FlexibleSpace();
            DrawCloseButton();

            GUILayout.Space(2);
            var mutedColor = GUI.contentColor;
            GUI.contentColor = PurpleMuted;
            GUILayout.BeginHorizontal();
            GUILayout.Label("v1.1", GUILayout.Height(14));
            GUILayout.FlexibleSpace();
            GUILayout.Label(string.IsNullOrEmpty(_tooltipText) ? "F5 to toggle" : _tooltipText, GUILayout.Height(14));
            GUILayout.EndHorizontal();
            GUI.contentColor = mutedColor;
            _tooltipText = GUI.tooltip;

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 24));
        }

        private void DrawTabBar()
        {
            var origBg = GUI.backgroundColor;
            var origContent = GUI.contentColor;

            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            for (int i = 0; i < _tabs.Length; i++)
            {
                var wasSelected = _currentTab == i;
                if (wasSelected)
                {
                    GUI.backgroundColor = Purple;
                    GUI.contentColor = Color.white;
                }
                else
                {
                    GUI.backgroundColor = new Color(0.02f, 0.0f, 0.04f);
                    GUI.contentColor = new Color(0.6f, 0.45f, 0.8f);
                }

                var isSelected = GUILayout.Toggle(wasSelected, _tabs[i], _skin.button, GUILayout.Height(26));
                if (isSelected && !wasSelected) _currentTab = i;
            }
            GUILayout.Space(4);
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            GUI.backgroundColor = origBg;
            GUI.contentColor = origContent;
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
            }
        }

        private void DrawCloseButton()
        {
            var orig = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.35f, 0.08f, 0.12f);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("Close (F5)", "Close the mod menu"), GUILayout.Width(120), GUILayout.Height(24)))
                _showMenu = false;
            GUILayout.Space(4);
            GUILayout.EndHorizontal();
            GUI.backgroundColor = orig;
        }

        // ──────────────── PLAYER TAB ────────────────
        private Vector2 _pscroll;
        private void DrawPlayerTab()
        {
            _pscroll = GUILayout.BeginScrollView(_pscroll, GUILayout.Width(_windowRect.width - 28), GUILayout.Height(_windowRect.height - 110));

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

            GUILayout.EndScrollView();
        }

        // ──────────────── ITEMS TAB ────────────────
        private Vector2 _iscroll;
        private string _itemSearch = "";
        private void DrawItemTab()
        {
            _iscroll = GUILayout.BeginScrollView(_iscroll, GUILayout.Width(_windowRect.width - 28), GUILayout.Height(_windowRect.height - 110));

            Section("Inventory");
            Item.InfiniteItems = Toggle(new GUIContent("Infinite Items", "Keeps item stacks at 10"), Item.InfiniteItems);
            if (Button("Give All Items (1 each)", "Adds one of every item to your inventory")) Item.GiveAllItems = true;
            if (DangerButton("Clear Inventory", "Removes all items from your inventory")) Item.ClearInventory();

            Section("Research & Crafting");
            if (Button("Unlock All Research", "Learn all crafting recipes instantly")) Item.UnlockAll = true;
            if (Button("Unlock All Blueprints", "Unlock all blueprints (may need save reload)")) Item.UnlockBlueprints = true;

            Section("Item Spawner");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(50));
            _itemSearch = GUILayout.TextField(_itemSearch);
            if (GUILayout.Button("x", GUILayout.Width(22), GUILayout.Height(20)))
                _itemSearch = "";
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (Button("Food", "Give food items")) Item.GiveCategory("Food");
            if (Button("Weapons", "Give weapons and tools")) Item.GiveCategory("Weapon");
            if (Button("Materials", "Give building materials")) Item.GiveCategory("Material");
            GUILayout.EndHorizontal();

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

            foreach (var item in filtered)
            {
                var sprite = item.settings_Inventory?.Sprite;
                var name = item.settings_Inventory?.DisplayName ?? item.UniqueName;

                GUILayout.BeginHorizontal();
                GUILayout.Space(4);

                var iconRect = GUILayoutUtility.GetRect(26, 26, GUILayout.Width(30));
                if (sprite != null)
                    DrawSprite(iconRect, sprite);

                GUILayout.Space(4);
                GUILayout.Label(name, GUILayout.Width(140));

                if (GUILayout.Button("+1", GUILayout.Width(34), GUILayout.Height(22)))
                    Item.SpawnItem(item.UniqueName, 1);
                if (GUILayout.Button("+10", GUILayout.Width(36), GUILayout.Height(22)))
                    Item.SpawnItem(item.UniqueName, 10);

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        // ──────────────── WORLD TAB ────────────────
        private Vector2 _wscroll;
        private void DrawWorldTab()
        {
            _wscroll = GUILayout.BeginScrollView(_wscroll, GUILayout.Width(_windowRect.width - 28), GUILayout.Height(_windowRect.height - 110));

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

            GUILayout.EndScrollView();
        }

        // ──────────────── RAFT TAB ────────────────
        private Vector2 _rscroll;
        private void DrawRaftTab()
        {
            _rscroll = GUILayout.BeginScrollView(_rscroll, GUILayout.Width(_windowRect.width - 28), GUILayout.Height(_windowRect.height - 110));

            Section("Raft Settings");
            InlineSlider("Max Speed", ref Raft.MaxSpeed, 1f, 30f, "");
            InlineSlider("Acceleration", ref Raft.Acceleration, 0.5f, 20f, "");

            Section("Raft Actions");
            if (Button("Teleport Raft to Player", "Brings the raft to your position")) Raft.TeleportToPlayer();
            if (Button("Teleport Player to Raft", "Sends you back to the raft")) World.TeleportToRaft();

            Section("Engine");
            Raft.InfiniteFuel = Toggle(new GUIContent("Infinite Fuel", "Engines never run out of fuel"), Raft.InfiniteFuel);
            Raft.AnchorAll = Toggle(new GUIContent("Auto Anchor", "Keeps all anchors deployed"), Raft.AnchorAll);

            GUILayout.EndScrollView();
        }

        // ──────────────── EXTRA TAB ────────────────
        private Vector2 _escroll;
        private int _selectedLandmark;
        private void DrawExtraTab()
        {
            _escroll = GUILayout.BeginScrollView(_escroll, GUILayout.Width(_windowRect.width - 28), GUILayout.Height(_windowRect.height - 110));

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

        private void DrawCoordinatesOverlay()
        {
            var orig = GUI.contentColor;
            GUI.contentColor = Purple;
            GUI.Label(new Rect(Screen.width - 180, 10, 170, 20), Extra.GetCoordinates());
            GUI.contentColor = orig;
        }

        private void Section(string title)
        {
            GUILayout.Space(4);
            GUILayout.Box(title, GUILayout.ExpandWidth(true), GUILayout.Height(22));
            GUILayout.Space(2);
        }

        private bool Toggle(GUIContent content, bool value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            var result = GUILayout.Toggle(value, content, _skin.toggle, GUILayout.Height(22));
            GUILayout.EndHorizontal();
            return result;
        }

        private bool Toggle(string label, bool value)
        {
            return Toggle(new GUIContent(label, ""), value);
        }

        private bool Button(string label, string tooltip = "")
        {
            bool clicked = false;
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            if (GUILayout.Button(new GUIContent(label, tooltip), _skin.button, GUILayout.Height(26)))
                clicked = true;
            GUILayout.EndHorizontal();
            return clicked;
        }

        private bool DangerButton(string label, string tooltip = "")
        {
            var orig = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.08f, 0.12f);
            bool clicked = false;
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            if (GUILayout.Button(new GUIContent(label, tooltip), _skin.button, GUILayout.Height(26)))
                clicked = true;
            GUILayout.EndHorizontal();
            GUI.backgroundColor = orig;
            return clicked;
        }

        private void InlineSlider(string label, ref float value, float min, float max, string suffix)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            GUILayout.Label(label, GUILayout.Width(60));
            value = GUILayout.HorizontalSlider(value, min, max, _skin.horizontalSlider, _skin.horizontalSliderThumb, GUILayout.Height(16));
            GUILayout.Label(value.ToString(suffix == "x" ? "F1" : "F0") + suffix, GUILayout.Width(45));
            GUILayout.Space(8);
            GUILayout.EndHorizontal();
        }

        private void LabelValue(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(8);
            GUILayout.Label(label + ": ", GUILayout.Width(70));
            var orig = GUI.contentColor;
            GUI.contentColor = Purple;
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
