using System;
using System.Collections;
using System.IO;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;

namespace RaftMod
{
    public class Updater
    {
        public bool UpdateAvailable { get; private set; }
        public string LatestVersion { get; private set; }
        public bool IsChecking { get; private set; }
        public bool IsDownloading { get; private set; }
        public bool DownloadDone { get; private set; }
        public string Error { get; private set; }

        private const string GITHUB_API = "https://api.github.com/repos/Code9914/raft-mod/releases/latest";
        private const string GITHUB_DL = "https://github.com/Code9914/raft-mod/releases/download";
        private const string DL_EXT = ".download";
        private const string PP_DOWNLOADED = "RaftMod_UpdateDownloaded";
        private const string PP_APPLIED = "RaftMod_UpdateApplied";

        public static string GetPendingVersion()
        {
            return PlayerPrefs.GetString(PP_DOWNLOADED, "");
        }

        public static string GetAppliedVersion()
        {
            return PlayerPrefs.GetString(PP_APPLIED, "");
        }

        public static void ClearAppliedVersion()
        {
            PlayerPrefs.DeleteKey(PP_APPLIED);
            PlayerPrefs.Save();
        }

        public static void ApplyPendingUpdate()
        {
            try
            {
                var pluginDir = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
                var downloadPath = Path.Combine(pluginDir, "RaftMod.dll" + DL_EXT);
                if (!File.Exists(downloadPath)) return;

                var pendingVersion = PlayerPrefs.GetString(PP_DOWNLOADED, "");
                if (string.IsNullOrEmpty(pendingVersion)) return;

                var currentPath = Path.Combine(pluginDir, "RaftMod.dll");
                var backupPath = Path.Combine(pluginDir, "RaftMod.dll.old");

                if (File.Exists(backupPath)) File.Delete(backupPath);
                File.Move(currentPath, backupPath);
                File.Move(downloadPath, currentPath);
                // Ne pas marquer la version comme "appliquée" ici : la DLL actuelle en mémoire
                // est toujours l'ancienne. On laisse la clé PP_DOWNLOADED afin que la
                // nouvelle instance du plugin (après redémarrage) puisse confirmer
                // l'application et enregistrer PP_APPLIED.
                Plugin.Log.LogInfo($"Update {pendingVersion} deployed on disk. Restart to load the new version.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"ApplyUpdate: {ex.Message}");
            }
        }

        // Appelée par la nouvelle instance du plugin au démarrage pour confirmer
        // que la mise à jour précédemment déployée a bien été chargée en mémoire.
        public static void ConfirmApplied(string currentVersion)
        {
            try
            {
                var pending = PlayerPrefs.GetString(PP_DOWNLOADED, "");
                if (string.IsNullOrEmpty(pending)) return;

                // Si la version en attente correspond à la version courante chargée,
                // alors on considère la mise à jour comme appliquée.
                if (CompareVersions(pending, currentVersion) == 0)
                {
                    PlayerPrefs.DeleteKey(PP_DOWNLOADED);
                    PlayerPrefs.SetString(PP_APPLIED, currentVersion);
                    PlayerPrefs.Save();
                    Plugin.Log.LogInfo($"Update {currentVersion} confirmed applied.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"ConfirmApplied: {ex.Message}");
            }
        }

        public IEnumerator CheckForUpdates(string currentVersion)
        {
            IsChecking = true;
            Error = null;
            LatestVersion = null;
            UpdateAvailable = false;

            Plugin.ClearLogFile();
            Plugin.UpdateLogFile("=== V\u00e9rification des mises \u00e0 jour ===");
            Plugin.UpdateLogFile("1/4 : Envoi de la requ\u00eate HTTP vers GitHub...");

            Plugin.Log.LogInfo("CheckForUpdates: starting UnityWebRequest...");
            using (var www = UnityWebRequest.Get(GITHUB_API))
            {
                www.SetRequestHeader("User-Agent", "RaftMod-Updater");
                www.timeout = 8;
                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    Error = www.error;
                    Plugin.Log.LogError($"CheckForUpdates: {www.error}");
                    Plugin.UpdateLogFile($"ERREUR HTTP : {www.error}");
                    Plugin.Instance.ShowPopup(Plugin.PopupType.Error, "Erreur r\u00e9seau",
                        $"Impossible de contacter GitHub : {www.error}");
                    IsChecking = false;
                    yield break;
                }

                Plugin.UpdateLogFile("2/4 : R\u00e9ponse re\u00e7ue de GitHub");
                Plugin.Log.LogInfo("CheckForUpdates: response received");
                yield return null;

                Plugin.UpdateLogFile("3/4 : Analyse de la r\u00e9ponse GitHub...");
                string json = www.downloadHandler.text;
                Plugin.Log.LogInfo($"JSON length: {json.Length}");
                yield return null;

                ParseVersion(json, currentVersion);
            }

            IsChecking = false;

            if (UpdateAvailable)
            {
                var applied = GetAppliedVersion();
                if (!string.IsNullOrEmpty(applied) && CompareVersions(LatestVersion, applied) == 0)
                {
                    Plugin.Log.LogInfo($"Update {LatestVersion} already applied.");
                    Plugin.UpdateLogFile($"Version {LatestVersion} d\u00e9j\u00e0 appliqu\u00e9e.");
                    Plugin.Instance.ShowPopup(Plugin.PopupType.Info, "D\u00e9j\u00e0 \u00e0 jour",
                        $"Version {LatestVersion} d\u00e9j\u00e0 install\u00e9e.", true);
                    yield break;
                }

                Plugin.Log.LogInfo($"Update {LatestVersion} found!");
                Plugin.UpdateLogFile($"Mise \u00e0 jour trouv\u00e9e : version {LatestVersion}");
                Plugin.UpdateLogFile("4/4 : T\u00e9l\u00e9chargement...");
                Plugin.Instance.ShowPopup(Plugin.PopupType.Info, "Mise \u00e0 jour disponible",
                    $"Version {LatestVersion} d\u00e9tect\u00e9e. T\u00e9l\u00e9chargement...");
                yield return Plugin.Instance.StartCoroutine(AutoDownload());
            }
            else
            {
                Plugin.Log.LogInfo("No update available - you have the latest version.");
                Plugin.UpdateLogFile("Aucune mise \u00e0 jour disponible - vous avez la derni\u00e8re version.");
                Plugin.UpdateLogFile("=== Fin ===");
                Plugin.Instance.ShowPopup(Plugin.PopupType.Info, "Derni\u00e8re version",
                    "Vous avez la version la plus r\u00e9cente.");
            }
        }

        private void ParseVersion(string json, string currentVersion)
        {
            var tagMarker = "\"tag_name\":\"";
            var idx = json.IndexOf(tagMarker);
            if (idx < 0) return;
            idx += tagMarker.Length;
            var endIdx = json.IndexOf("\"", idx);
            if (endIdx <= idx) return;
            LatestVersion = json.Substring(idx, endIdx - idx);
            UpdateAvailable = CompareVersions(LatestVersion, currentVersion) > 0;
        }

        private IEnumerator AutoDownload()
        {
            if (string.IsNullOrEmpty(LatestVersion)) yield break;

            IsDownloading = true;
            Error = null;

            var downloadUrl = $"{GITHUB_DL}/{LatestVersion}/RaftMod.dll";
            var tempPath = Path.GetTempFileName() + ".dll";
            var pluginDir = Path.GetDirectoryName(typeof(Plugin).Assembly.Location);
            var targetPath = Path.Combine(pluginDir, "RaftMod.dll" + DL_EXT);

            Plugin.UpdateLogFile($"T\u00e9l\u00e9chargement depuis {downloadUrl}");
            Plugin.Log.LogInfo($"Downloading {downloadUrl}...");
            using (var www = UnityWebRequest.Get(downloadUrl))
            {
                www.SetRequestHeader("User-Agent", "RaftMod-Updater");
                www.timeout = 15;
                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    Error = www.error;
                    Plugin.Log.LogError($"Download failed: {www.error}");
                    Plugin.UpdateLogFile($"ERREUR t\u00e9l\u00e9chargement : {www.error}");
                    Plugin.Instance.ShowPopup(Plugin.PopupType.Error, "Erreur t\u00e9l\u00e9chargement",
                        $"Impossible de t\u00e9l\u00e9charger la mise \u00e0 jour : {www.error}");
                }
                else
                {
                    Plugin.UpdateLogFile("Fichier re\u00e7u, sauvegarde sur le disque...");
                    Plugin.Log.LogInfo("Download received, saving to disk...");
                    try
                    {
                        File.WriteAllBytes(tempPath, www.downloadHandler.data);
                        if (File.Exists(targetPath)) File.Delete(targetPath);
                        File.Move(tempPath, targetPath);
                        DownloadDone = true;
                        PlayerPrefs.SetString(PP_DOWNLOADED, LatestVersion);
                        PlayerPrefs.Save();
                        Plugin.UpdateLogFile($"T\u00e9l\u00e9chargement termin\u00e9 ! Version {LatestVersion} pr\u00eate \u00e0 \u00eatre install\u00e9e.");
                        Plugin.UpdateLogFile("=== Fin ===");
                        Plugin.Log.LogInfo($"Update {LatestVersion} downloaded.");
                        Plugin.Instance.ShowPopup(Plugin.PopupType.Success, "T\u00e9l\u00e9chargement termin\u00e9 !",
                            $"Version {LatestVersion} t\u00e9l\u00e9charg\u00e9e. Red\u00e9marrez le jeu pour appliquer.", true);
                    }
                    catch (Exception ex)
                    {
                        Error = ex.Message;
                        Plugin.Log.LogError($"Save failed: {ex.Message}");
                        Plugin.UpdateLogFile($"ERREUR sauvegarde fichier : {ex.Message}");
                        Plugin.Instance.ShowPopup(Plugin.PopupType.Error, "Erreur sauvegarde",
                            $"Impossible de sauvegarder le fichier : {ex.Message}");
                    }
                }
            }

            IsDownloading = false;
            try { if (File.Exists(tempPath)) File.Delete(tempPath); }
            catch { }
        }

        private static int CompareVersions(string v1, string v2)
        {
            v1 = v1.TrimStart('v');
            v2 = v2.TrimStart('v');

            var p1 = v1.Split('.');
            var p2 = v2.Split('.');

            for (int i = 0; i < Math.Max(p1.Length, p2.Length); i++)
            {
                int a = i < p1.Length ? int.Parse(p1[i]) : 0;
                int b = i < p2.Length ? int.Parse(p2[i]) : 0;
                if (a > b) return 1;
                if (a < b) return -1;
            }
            return 0;
        }
    }
}
