using System;
using System.Collections;
using System.IO;
using System.Net;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace RaftMod
{
    [BepInPlugin("raft.mod", "Raft Mod UI", "1.8.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static string Version => "1.8.0";
        internal static Updater ModUpdater;
        internal static Plugin Instance;

        internal enum PopupState { None, UpdateApplied, Step1Init, Step2RequestSent, Step3ResponseOk, Step4Parsing, Step5Downloading, Step6Downloaded, Error }
        private PopupState _popup = PopupState.Step1Init;
        private string _popupVersion;
        private string _popupDetail;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Updater.ApplyPendingUpdate();

            ModUpdater = new Updater();
            ModUI.Instance = new ModUI();
            Logger.LogInfo("RaftMod loaded!");
            StartCoroutine(StartupCheck());
        }

        private IEnumerator StartupCheck()
        {
            yieldMz80.008.1.0.1
1.8.0
            yield return new WaitForSeconds(1f);
            UpdateLogFile("D\u00e9marrage de la v\u00e9rification des mises \u00e0 jour...");
            _popup = PopupState.Step2RequestSent;
            _popupDetail = "D\u00e9marrage de la r\u00e9quete HTTP...";
            yield return StartCoroutine(ModUpdater.CheckForUpdates(Version));
        }

        public void SetPopupStep(PopupState state, string detail)
        {
            _popup = state;
            _popupDetail = detail;
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
                DrawPopup();
                ModUI.Instance?.OnGUI();
            }
            catch (Exception ex) { Logger.LogError($"RaftMod GUI: {ex.Message}"); }
        }

        private void DrawPopup()
        {
            if (_popup == PopupState.None) return;

            var sw = Screen.width;
            var hasBtn = _popup == PopupState.UpdateApplied || _popup == PopupState.Step6Downloaded;
            var noDismiss = _popup == PopupState.Step1Init || _popup == PopupState.Step2RequestSent || _popup == PopupState.Step3ResponseOk || _popup == PopupState.Step4Parsing || _popup == PopupState.Step5Downloading;
            var boxW = 380f;
            var boxH = hasBtn ? 120f : 90f;
            var boxRect = new Rect((sw - boxW) / 2f, 40f, boxW, boxH);

            GUI.DrawTexture(boxRect, MakeTex(1, 1, new Color(0.05f, 0.02f, 0.08f, 0.95f)));
            GUI.DrawTexture(new Rect(boxRect.x, boxRect.yMax - 2, boxRect.width, 2), MakeTex(1, 1, new Color(0.5f, 0.25f, 0.9f)));

            var orig = GUI.contentColor;
            string title;
            switch (_popup)
            {
                case PopupState.UpdateApplied:
                    GUI.contentColor = new Color(0.3f, 1.0f, 0.3f);
                    title = "Mise \u00e0 jour install\u00e9e !";
                    break;
                case PopupState.Step6Downloaded:
                    GUI.contentColor = new Color(0.3f, 1.0f, 0.3f);
                    title = "T\u00e9l\u00e9chargement termin\u00e9 !";
                    break;
                case PopupState.Error:
                    GUI.contentColor = Color.red;
                    title = "Erreur";
                    break;
                default:
                    GUI.contentColor = new Color(0.65f, 0.42f, 1.0f);
                    title = "Mise \u00e0 jour";
                    break;
            }
            GUI.contentColor = orig;

            GUI.Label(new Rect(boxRect.x + 12, boxRect.y + 8, boxRect.width - 24, 22), title,
                new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = GUI.contentColor } });

            GUI.contentColor = new Color(0.92f, 0.88f, 0.96f);
            string msg;
            if (_popup == PopupState.Error)
                msg = ModUpdater?.Error ?? "Erreur inconnue.\nAppuyez sur une touche pour fermer.";
            else if (_popup == PopupState.UpdateApplied)
                msg = $"Red\u00e9marrez pour utiliser la version {_popupVersion}.";
            else if (_popup == PopupState.Step6Downloaded)
                msg = $"Version {_popupVersion} pr\u00eate \u00e0 \u00eatre install\u00e9e.";
            else
                msg = _popupDetail;

            GUI.Label(new Rect(boxRect.x + 12, boxRect.y + 34, boxRect.width - 24, 40), msg);
            GUI.contentColor = orig;

            if (hasBtn)
            {
                var btnRect = new Rect((boxRect.width - 140f) / 2f, boxRect.y + 72f, 140f, 28f);
                var bg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.5f, 0.25f, 0.9f);
                if (GUI.Button(btnRect, "Red\u00e9marrer", new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold }))
                {
                    _popup = PopupState.None;
                    Application.Quit();
                }
                GUI.backgroundColor = bg;
            }
            else if (!noDismiss && (Event.current.type == EventType.KeyDown || Event.current.type == EventType.MouseDown))
                _popup = PopupState.None;
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
