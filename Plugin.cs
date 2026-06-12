using System;
using BepInEx;
using BepInEx.Logging;

namespace RaftMod
{
    [BepInPlugin("raft.mod", "Raft Mod UI", "1.2.1")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            ModUI.Instance = new ModUI();
            Logger.LogInfo("RaftMod loaded!");
        }

        private void Update()
        {
            try { ModUI.Instance?.Update(); }
            catch (Exception ex) { Logger.LogError($"RaftMod: {ex.Message}"); }
        }

        private void OnGUI()
        {
            try { ModUI.Instance?.OnGUI(); }
            catch (Exception ex) { Logger.LogError($"RaftMod GUI: {ex.Message}"); }
        }
    }
}
