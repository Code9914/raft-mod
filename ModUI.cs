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
        private bool _lastShowMenu;
        private int _currentTab;
        private bool _gameReady;
        private float _readyTimer;
        private Rect _windowRect = new Rect(50, 50, 540, 710);

        private readonly string[] _tabs = { "Joueur", "Objets", "Monde", "Radeau", "Extra", "Contrôles" };

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
                fontStyle = FontStyle.Normal,
                wordWrap = false
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

            if (_showMenu != _lastShowMenu)
            {
                _lastShowMenu = _showMenu;
            }

            if (_showMenu && !Input.mousePresent)
                return;

            if (_showMenu && _windowRect.Contains(Input.mousePosition))
            {
                GUI.DragWindow(new Rect(_windowRect.x, _windowRect.y, _windowRect.width, 28));
            }

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

            Add("menu_toggle", "Menu", "Ouvrir/fermer le menu du mod", KeyCode.F5, () => { _showMenu = !_showMenu; Extra.MenuOpen = _showMenu; });
            Add("noclip", "Noclip", "Activer/d\u00e9sactiver le mode vol", KeyCode.None, () => { Player.NoClip = !Player.NoClip; });
            Add("godmode", "Mode Dieu", "Activer/d\u00e9sactiver le mode invincible", KeyCode.None, () => { Player.GodMode = !Player.GodMode; });
            Add("coordinates", "Coordonn\u00e9es", "Afficher/masquer les coordonn\u00e9es", KeyCode.None, () => { Extra.ShowCoordinates = !Extra.ShowCoordinates; });
            Add("thirdperson", "3e Personne", "Activer/d\u00e9sactiver la cam\u00e9ra 3e personne", KeyCode.None, () => { Extra.ThirdPerson = !Extra.ThirdPerson; });
            Add("infinite_items", "Items Infinis", "Activer/d\u00e9sactiver les objets infinis", KeyCode.None, () => { Item.InfiniteItems = !Item.InfiniteItems; });
            Add("creative", "Mode Cr\u00e9atif", "Activer/d\u00e9sactiver le mode cr\u00e9atif", KeyCode.None, () => { Extra.SetCreativeMode(!Extra.IsCreativeMode()); });
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

            var origC = GUI.contentColor;
            GUI.contentColor = TextDim;
            GUI.Label(new Rect(14, 5, 200, 20), "Menu Raft Mod");
            GUI.contentColor = Accent;
            var verStyle = new GUIStyle(_skin.label) { fontSize = 9 };
            GUI.Label(new Rect(_windowRect.width - 80, 7, 70, 16), Plugin.Version, verStyle);
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

            Section("Sant\u00e9");
            Player.GodMode = Toggle(new GUIContent("Mode Dieu", "Invincible, vie et bonus de sant\u00e9 illimit\u00e9s"), Player.GodMode);
            if (Button("Soin Complet", "Restaure la sant\u00e9 et le bonus HP au max")) Player.HealPlayer();
            LabelValue("Sant\u00e9", $"{Player.GetHealth():F0}");
            LabelValue("HP Bonus", $"{Player.GetBonusHealth():F0}");

            Section("Survie");
            Player.InfiniteOxygen = Toggle(new GUIContent("Oxyg\u00e8ne Infini", "L'oxyg\u00e8ne ne s'\u00e9puise jamais"), Player.InfiniteOxygen);
            Player.InfiniteHunger = Toggle(new GUIContent("Faim/Soif Infinie", "La faim et la soif restent au max"), Player.InfiniteHunger);
            Player.NoHungerLoss = Toggle(new GUIContent("Pas de Perte Faim/Soif", "D\u00e9sactive la baisse de faim et soif"), Player.NoHungerLoss);

            Section("D\u00e9placement");
            Player.NoClip = Toggle(new GUIContent("Noclip / Vol", "Cam\u00e9ra libre, sans collision"), Player.NoClip);
            InlineSlider("Vitesse", ref Player.MoveSpeed, 0.5f, 20f, "x");
            InlineSlider("Saut", ref Player.JumpMultiplier, 0.5f, 10f, "x");
            InlineSlider("Nage", ref Player.SwimMultiplier, 0.5f, 10f, "x");
            InlineSlider("Gravit\u00e9", ref Player.Gravity, 5f, 50f, "");

            Section("Combat");
            Player.InfDurability = Toggle(new GUIContent("Durabilit\u00e9 Infinie", "Les outils et armes ne perdent jamais de durabilit\u00e9"), Player.InfDurability);
            Player.NoFallDamage = Toggle(new GUIContent("Pas de D\u00e9g\u00e2ts de Chute", "Annule tous les d\u00e9g\u00e2ts de chute"), Player.NoFallDamage);
            InlineSlider("Multiplicateur D\u00e9g\u00e2ts", ref Player.DamageMultiplier, 0f, 20f, "x");

            Section("Confort");
            Player.AutoPickup = Toggle(new GUIContent("Ramassage Auto", "Ramasser automatiquement les objets proches"), Player.AutoPickup);
            InlineSlider("Champ de Vision", ref Player.FOV, 40f, 120f, "");

            GUILayout.EndScrollView();
        }

        // ──────────────── ITEMS TAB ────────────────
        private Vector2 _iscroll;
        private string _itemSearch = "";
        private void DrawItemTab()
        {
            _iscroll = GUILayout.BeginScrollView(_iscroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Section("Inventaire");
            Item.InfiniteItems = Toggle(new GUIContent("Objets Infinis", "Les piles d'objets restent \u00e0 10"), Item.InfiniteItems);
            if (Button("Tous les Objets (1 chacun)", "Ajoute un exemplaire de chaque objet dans votre inventaire")) Item.GiveAllItems = true;
            if (DangerButton("Vider l'Inventaire", "Supprime tous les objets de votre inventaire")) Item.ClearInventory();

            Section("Recherche & Artisanat");
            if (Button("D\u00e9verrouiller toutes les Recherches", "Apprendre toutes les recettes d'artisanat")) Item.UnlockAll = true;
            if (Button("D\u00e9verrouiller tous les Plans", "D\u00e9bloquer tous les plans (peut n\u00e9cessiter un rechargement)")) Item.UnlockBlueprints = true;

            Section("G\u00e9n\u00e9rateur d'Objets");

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

            Section("Temps");
            World.AlwaysDay = Toggle(new GUIContent("Toujours le Jour", "Force l'heure \u00e0 midi"), World.AlwaysDay);
            LabelValue("Heure Actuelle", $"{World.GetCurrentHour():F1}h");
            InlineSlider("Cible", ref World.TargetHour, 0f, 24f, "h");
            if (Button("D\u00e9finir l'Heure", $"R\u00e9gler l'heure sur {World.TargetHour:F0}:00")) World.SetHour(World.TargetHour);
            InlineSlider("Vitesse", ref World.TimeSpeed, 0f, 10f, "x");

            Section("M\u00e9t\u00e9o");
            GUILayout.BeginHorizontal();
            if (Button("D\u00e9gag\u00e9", "Temps d\u00e9gag\u00e9")) World.SetWeather(0);
            if (Button("Pluie", "Temps pluvieux")) World.SetWeather(1);
            if (Button("Temp\u00eate", "Forte temp\u00eate")) World.SetWeather(2);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (Button("Brouillard", "Temps brumeux")) World.SetWeather(3);
            if (Button("Neige", "Temps neigeux")) World.SetWeather(4);
            if (Button("Calme", "Mer calme")) World.SetWeather(5);
            GUILayout.EndHorizontal();

            Section("Entit\u00e9s");
            World.NoEnemies = Toggle(new GUIContent("Pas d'Ennemis", "Tue automatiquement tous les ennemis chaque seconde"), World.NoEnemies);
            World.NoShark = Toggle(new GUIContent("D\u00e9sactiver Requin", "Tue automatiquement Bruce le requin"), World.NoShark);
            if (DangerButton("Tuer tous les Ennemis", "Tue instantan\u00e9ment tous les ennemis")) World.KillAllEnemies();

            Section("T\u00e9l\u00e9portation");
            if (Button("T\u00e9l\u00e9porter au Radeau", "Se t\u00e9l\u00e9porter sur le radeau")) World.TeleportToRaft();
            if (Button("T\u00e9l\u00e9porter au Viseur", "Se t\u00e9l\u00e9porter l\u00e0 o\u00f9 vous regardez")) World.TeleportToCamera();

            Section("P\u00eache");
            World.InstantFish = Toggle(new GUIContent("Poisson Instantan\u00e9", "Le poisson mord d\u00e8s que vous lancez"), World.InstantFish);
            World.AutoCatch = Toggle(new GUIContent("Attraper Auto", "Enroule automatiquement le poisson quand il mord"), World.AutoCatch);

            GUILayout.EndScrollView();
        }

        // ──────────────── RAFT TAB ────────────────
        private Vector2 _rscroll;
        private void DrawRaftTab()
        {
            _rscroll = GUILayout.BeginScrollView(_rscroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Section("Param\u00e8tres du Radeau");
            InlineSlider("Vitesse Max", ref Raft.MaxSpeed, 1f, 30f, "");
            InlineSlider("Acc\u00e9l\u00e9ration", ref Raft.Acceleration, 0.5f, 20f, "");

            Section("Actions du Radeau");
            if (Button("T\u00e9l\u00e9porter le Radeau au Joueur", "Am\u00e8ne le radeau \u00e0 votre position")) Raft.TeleportToPlayer();
            if (Button("T\u00e9l\u00e9porter le Joueur au Radeau", "Vous renvoie sur le radeau")) World.TeleportToRaft();

            Section("Moteur");
            Raft.InfiniteFuel = Toggle(new GUIContent("Carburant Infini", "Les moteurs ne manquent jamais de carburant"), Raft.InfiniteFuel);
            Raft.AnchorAll = Toggle(new GUIContent("Ancre Auto", "Maintient toutes les ancres d\u00e9ploy\u00e9es"), Raft.AnchorAll);

            Section("Protection");
            Raft.AutoRepair = Toggle(new GUIContent("R\u00e9paration Auto", "R\u00e9pare automatiquement les blocs du radeau"), Raft.AutoRepair);

            GUILayout.EndScrollView();
        }

        // ──────────────── EXTRA TAB ────────────────
        private Vector2 _escroll;
        private int _selectedLandmark;
        private void DrawExtraTab()
        {
            _escroll = GUILayout.BeginScrollView(_escroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Section("Mode de Jeu");
            var wasCreative = Extra.IsCreativeMode();
            var nowCreative = Toggle(new GUIContent("Mode Cr\u00e9atif", "D\u00e9sactive la faim, construction instantan\u00e9e, pas de co\u00fbt"), wasCreative);
            if (nowCreative != wasCreative) Extra.SetCreativeMode(nowCreative);
            InlineSlider("Vitesse du Jeu", ref Extra.GameSpeed, 0.1f, 10f, "x");

            Section("Progression");
            if (Button("D\u00e9verrouiller tous les Succ\u00e8s", "D\u00e9bloquer chaque succ\u00e8s Steam")) Extra.UnlockAllAchievements();
            if (Button("D\u00e9verrouiller toutes les Notes", "D\u00e9couvrir toutes les notes d'histoire")) Extra.UnlockAllNotes();
            if (Button("D\u00e9verrouiller toutes les Fr\u00e9quences", "Activer toutes les fr\u00e9quences radio")) Extra.UnlockAllFrequencies();
            if (Button("Terminer toutes les Qu\u00eates", "Terminer toutes les qu\u00eates d'histoire instantan\u00e9ment")) Extra.FinishAllQuests();

            if (Button("D\u00e9verrouiller tous les Personnages", "D\u00e9bloquer tous les personnages jouables")) Extra.UnlockAllCharacters();

            Section("Plantes");
            if (Button("Pousses Instantan\u00e9es", "Ignorer le temps de croissance des cultures")) Extra.InstantGrowPlants();
            if (Button("R\u00e9colter toutes les Plantes", "R\u00e9colter toutes les cultures matures")) Extra.HarvestAllPlants();

            Section("Utilitaires");
            Extra.ShowCoordinates = Toggle(new GUIContent("Afficher Coordonn\u00e9es", "Afficher les coordonn\u00e9es \u00e0 l'\u00e9cran"), Extra.ShowCoordinates);
            Extra.InfiniteBattery = Toggle(new GUIContent("Batterie Infinie", "Toutes les batteries restent charg\u00e9es"), Extra.InfiniteBattery);

            Section("T\u00e9l\u00e9porter au Point d'Int\u00e9r\u00eat");
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
                if (Button("T\u00e9l\u00e9porter", $"T\u00e9l\u00e9porter vers {lmName}")) Extra.TeleportToLandmark(sel);
            }
            else
            {
                GUILayout.Label("Aucun point d'int\u00e9r\u00eat trouv\u00e9 \u00e0 proximit\u00e9");
            }

            GUILayout.EndScrollView();
        }

        private Vector2 _cscroll;
        private void DrawControlsTab()
        {
            _cscroll = GUILayout.BeginScrollView(_cscroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Section("Raccourcis Clavier");
            GUILayout.Space(2);
            var origC = GUI.contentColor;
            GUI.contentColor = TextDim;
            GUILayout.Label("Cliquez sur une touche pour la modifier. ESC=annuler, SUPPR=effacer.");
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
            GUILayout.Label(label, GUILayout.ExpandWidth(false));
            GUILayout.Space(4);
            value = GUILayout.HorizontalSlider(value, min, max, _skin.horizontalSlider, _skin.horizontalSliderThumb, GUILayout.ExpandWidth(true), GUILayout.Height(16));
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
            GUILayout.Label(label + ": ", GUILayout.ExpandWidth(false));
            var orig = GUI.contentColor;
            GUI.contentColor = Accent;
            GUILayout.Label(value, GUILayout.ExpandWidth(false));
            GUI.contentColor = orig;
            GUILayout.FlexibleSpace();
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
