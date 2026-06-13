using System;
using System.Collections;
using System.IO;
using System.Net;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace RaftMod
{
    [BepInPlugin("raft.mod", "Raft Mod UI", "1.8.5")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static string Version => "1.8.5";
        internal static Updater ModUpdater;
        internal static Plugin Instance;

        internal enum PopupType { None, Info, Success, Error }
        private PopupType _popupType;
        private string _popupTitle;
        private string _popupMsg;
        private bool _popupHasBtn;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Updater.ApplyPendingUpdate();
            // Après avoir tenté d'appliquer un update sur le disque, confirmer
            // si la DLL actuellement chargée correspond à la version téléchargée.
            Updater.ConfirmApplied(Version);

            ModUpdater = new Updater();
            ModUI.Instance = new ModUI();
            Logger.LogInfo("RaftMod loaded!");
            StartCoroutine(StartupCheck());
        }

        private IEnumerator StartupCheck()
        {
            yield return new WaitForSeconds(1f);
            UpdateLogFile("D\u00e9marrage de la v\u00e9rification des mises \u00e0 jour...");
            ShowPopup(PopupType.Info, "Mise \u00e0 jour", "V\u00e9rification...");
            yield return StartCoroutine(ModUpdater.CheckForUpdates(Version));
            UpdateLogFile("V\u00e9rification termin\u00e9e.");
        }

        private void Update()
        {
            try { ModUI.Instance?.Update(); }
            catch (Exception ex) { Logger.LogError($"RaftMod: {ex.Message}"); }
        }

        private void OnGUI()
        {
            try
            {
                ModUI.Instance?.OnGUI();
                DrawPopup();
            }
            catch (Exception ex) { Logger.LogError($"RaftMod GUI: {ex.Message}"); }
        }

        private void DrawPopup()
        {
            if (_popupType == PopupType.None) return;

            var boxW = 400f;
            var boxH = _popupHasBtn ? 130f : 95f;
            var boxRect = new Rect((Screen.width - boxW) / 2f, 40f, boxW, boxH);

            var bg = new Color(0.05f, 0.02f, 0.08f, 0.95f);
            var border = new Color(0.5f, 0.25f, 0.9f);
            GUI.DrawTexture(boxRect, MakeTex(1, 1, bg));
            GUI.DrawTexture(new Rect(boxRect.x, boxRect.yMax - 2, boxRect.width, 2), MakeTex(1, 1, border));

            Color col;
            switch (_popupType)
            {
                case PopupType.Success: col = new Color(0.3f, 1.0f, 0.3f); break;
                case PopupType.Error: col = Color.red; break;
                default: col = new Color(0.65f, 0.42f, 1.0f); break;
            }

            var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = col;
            GUI.Label(new Rect(boxRect.x + 14, boxRect.y + 10, boxRect.width - 28, 22), _popupTitle, titleStyle);
            GUI.Label(new Rect(boxRect.x + 14, boxRect.y + 36, boxRect.width - 28, 40), _popupMsg);

            if (_popupHasBtn)
            {
                var btnW = 140f;
                var btnH = 30f;
                var btnRect = new Rect(boxRect.x + (boxW - btnW) / 2f, boxRect.y + boxH - btnH - 12, btnW, btnH);
                GUI.backgroundColor = new Color(0.5f, 0.25f, 0.9f);
                if (GUI.Button(btnRect, "Red\u00e9marrer", new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold }))
                {
                    _popupType = PopupType.None;
                    Application.Quit();
                }
                GUI.backgroundColor = Color.white;
            }
            else if (Event.current.type == EventType.KeyDown || Event.current.type == EventType.MouseDown)
                _popupType = PopupType.None;
        }

        internal void ShowPopup(PopupType type, string title, string msg, bool hasBtn = false)
        {
            _popupType = type;
            _popupTitle = title;
            _popupMsg = msg;
            _popupHasBtn = hasBtn;
        }

        internal static void UpdateLogFile(string message)
        {
            try
            {
                var logPath = Path.Combine(Paths.ConfigPath, "RaftMod-update.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        internal static void ClearLogFile()
        {
            try
            {
                var logPath = Path.Combine(Paths.ConfigPath, "RaftMod-update.log");
                File.WriteAllText(logPath, "");
            }
            catch { }
        }

        private static Texture2D MakeTex(int w, int h, Color c)
        {
            var t = new Texture2D(w, h);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    t.SetPixel(x, y, c);
            t.Apply();
            return t;
        }
    }
}