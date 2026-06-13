using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RaftMod
{
    public class ExtraFeatures
    {
        public bool ShowCoordinates;
        public bool ThirdPerson;
        public bool InfiniteBattery;
        public bool MenuOpen;
        public float GameSpeed = 1f;

        private bool _wasThirdPerson;
        private Vector3 _originalCamPos;

        private Network_Player _localPlayer;
        private Network_Player LocalPlayer
        {
            get
            {
                if (_localPlayer == null)
                {
                    try { _localPlayer = ComponentManager<Network_Player>.Value; }
                    catch { }
                }
                return _localPlayer;
            }
        }

        private string[] _landmarkNames = new string[0];
        private float _landmarkRefresh;
        private Battery[] _cachedBatteries = new Battery[0];
        private float _batteryRefresh;

        public void Update()
        {
            try
            {
                if (Math.Abs(GameSpeed - 1f) > 0.01f)
                    Time.timeScale = GameSpeed;
                else if (Math.Abs(Time.timeScale - 1f) > 0.01f)
                    Time.timeScale = 1f;

                if (ThirdPerson)
                {
                    if (MenuOpen)
                        ResetThirdPerson();
                    else
                        HandleThirdPerson();
                }
                else if (_wasThirdPerson)
                {
                    ResetThirdPerson();
                }

                _landmarkRefresh -= Time.deltaTime;
                if (_landmarkRefresh <= 0f)
                {
                    RefreshLandmarks();
                    _landmarkRefresh = 2f;
                }

                if (InfiniteBattery)
                {
                    _batteryRefresh -= Time.deltaTime;
                    if (_batteryRefresh <= 0f)
                    {
                        _cachedBatteries = UnityEngine.Object.FindObjectsOfType<Battery>();
                        _batteryRefresh = 2f;
                    }

                    RefillAllBatteries();
                }
                else if (_cachedBatteries.Length > 0)
                {
                    _cachedBatteries = new Battery[0];
                    _batteryRefresh = 0f;
                }
            }
            catch (Exception ex) { Plugin.Log.LogError($"Extra.Update: {ex.Message}"); }
        }

        private void RefreshLandmarks()
        {
            try
            {
                var list = WorldManager.AllLandmarks;
                if (list != null && list.Count > 0)
                {
                    _landmarkNames = list
                        .Where(l => l != null && l.isSpawned)
                        .Select(l => l.gameObject.name.Replace("(Clone)", "").Trim())
                        .ToArray();
                }
            }
            catch { }
        }

        // ──── ACHIEVEMENTS ────

        public void UnlockAllAchievements()
        {
            try
            {
                var types = Enum.GetValues(typeof(AchievementType));
                int count = 0;
                foreach (AchievementType type in types)
                {
                    try
                    {
                        if (!AchievementHandler.HasUnlocked(type))
                        {
                            AchievementHandler.UnlockAchievement(type);
                            count++;
                        }
                    }
                    catch { }
                }
                Plugin.Log.LogInfo($"Unlocked {count} achievements!");
            }
            catch (Exception ex) { Plugin.Log.LogError($"UnlockAchievements: {ex.Message}"); }
        }

        // ──── NOTES & FREQUENCIES ────

        public void UnlockAllNotes()
        {
            try
            {
                var nb = UnityEngine.Object.FindObjectOfType<NoteBook>();
                if (nb != null)
                {
                    nb.UnlockAllNotes();
                    Plugin.Log.LogInfo("All notes unlocked!");
                }
            }
            catch (Exception ex) { Plugin.Log.LogError($"UnlockNotes: {ex.Message}"); }
        }

        public void UnlockAllFrequencies()
        {
            try
            {
                var types = new ChunkPointType[]
                {
                    ChunkPointType.Landmark_RadioTower,
                    ChunkPointType.Landmark_Vasagatan,
                    ChunkPointType.Landmark_Balboa,
                    ChunkPointType.Landmark_CaravanIsland,
                    ChunkPointType.Landmark_Tangaroa,
                    ChunkPointType.Landmark_VarunaPoint,
                    ChunkPointType.Landmark_Temperance,
                    ChunkPointType.Landmark_Utopia
                };
                foreach (var t in types)
                {
                    if (!NoteBook.HasUnlockedFrequencyNoteAtDestination(t))
                        NoteBook.UnlockFrequency(t);
                }
                Plugin.Log.LogInfo("All frequencies unlocked!");
            }
            catch (Exception ex) { Plugin.Log.LogError($"UnlockFrequencies: {ex.Message}"); }
        }

        // ──── CHARACTER ────

        public void UnlockAllCharacters()
        {
            try
            {
                CharacterManager.UnlockAllCharacters();
                Plugin.Log.LogInfo("All characters unlocked!");
            }
            catch (Exception ex) { Plugin.Log.LogError($"UnlockChars: {ex.Message}"); }
        }

        // ──── QUESTS ────

        public void FinishAllQuests()
        {
            try
            {
                var types = Enum.GetValues(typeof(QuestType));
                int count = 0;
                foreach (QuestType type in types)
                {
                    if (type != QuestType.None)
                    {
                        try
                        {
                            if (!QuestProgressTracker.HasFinishedQuest(type))
                            {
                                QuestProgressTracker.FinishQuest(type);
                                count++;
                            }
                        }
                        catch { }
                    }
                }
                Plugin.Log.LogInfo($"Finished {count} quests!");
            }
            catch (Exception ex) { Plugin.Log.LogError($"Quests: {ex.Message}"); }
        }

        // ──── GAME MODE ────

        private bool _creativeMode;

        public bool IsCreativeMode()
        {
            try
            {
                var val = GameModeValueManager.GetCurrentGameModeValue();
                return val != null && val.name != null && val.name.ToLower().Contains("creative");
            }
            catch { return _creativeMode; }
        }

        public void SetCreativeMode(bool on)
        {
            try
            {
                _creativeMode = on;
                GameModeValueManager.SelectCurrentGameMode(on ? GameMode.Creative : GameMode.Normal);
                Plugin.Log.LogInfo($"Creative mode: {on}");
            }
            catch (Exception ex) { Plugin.Log.LogError($"Creative: {ex.Message}"); }
        }

        // ──── PLANTS ────

        public void InstantGrowPlants()
        {
            try
            {
                var pm = UnityEngine.Object.FindObjectOfType<PlantManager>();
                if (pm != null)
                {
                    pm.ForwardTime(99999f);
                    Plugin.Log.LogInfo("Plants grown!");
                }
            }
            catch (Exception ex) { Plugin.Log.LogError($"GrowPlants: {ex.Message}"); }
        }

        public void HarvestAllPlants()
        {
            try
            {
                var pm = UnityEngine.Object.FindObjectOfType<PlantManager>();
                if (pm == null) return;
                int count = 0;
                foreach (var plot in PlantManager.allCropplots)
                {
                    if (plot == null) continue;
                    foreach (var slot in plot.plantationSlots)
                    {
                        if (slot != null && slot.plant != null && slot.plant.FullyGrown() && slot.plant.harvestable)
                        {
                            pm.Harvest(slot.plant, true);
                            count++;
                        }
                    }
                }
                Plugin.Log.LogInfo($"Harvested {count} plants!");
            }
            catch (Exception ex) { Plugin.Log.LogError($"Harvest: {ex.Message}"); }
        }

        // ──── TELEPORT TO LANDMARKS ────

        public string[] GetLandmarkNames()
        {
            return _landmarkNames;
        }

        public int GetLandmarkCount()
        {
            return _landmarkNames.Length;
        }

        public void TeleportToLandmark(int index)
        {
            try
            {
                var list = WorldManager.AllLandmarks;
                var spawned = list.Where(l => l != null && l.isSpawned).ToList();
                if (index < 0 || index >= spawned.Count) return;
                var lm = spawned[index];
                if (lm == null) return;
                var net = LocalPlayer;
                if (net != null)
                    net.transform.position = lm.transform.position + Vector3.up * 3f;
            }
            catch (Exception ex) { Plugin.Log.LogError($"TP Landmark: {ex.Message}"); }
        }

        // ──── COORDINATES ────

        public string GetCoordinates()
        {
            try
            {
                var net = LocalPlayer;
                if (net != null)
                {
                    var pos = net.transform.position;
                    return $"X: {pos.x:F1}  Y: {pos.y:F1}  Z: {pos.z:F1}";
                }
            }
            catch { }
            return "---";
        }

        // ──── THIRD PERSON ────

        private void HandleThirdPerson()
        {
            try
            {
                var net = LocalPlayer;
                if (net?.Camera == null) return;
                var camTransform = net.Camera.transform;
                if (!_wasThirdPerson)
                {
                    _originalCamPos = camTransform.localPosition;
                    _wasThirdPerson = true;
                }
                camTransform.localPosition = Vector3.Lerp(camTransform.localPosition, new Vector3(0f, 1.5f, -5f), Time.deltaTime * 4f);
            }
            catch { }
        }

        private void ResetThirdPerson()
        {
            try
            {
                var net = LocalPlayer;
                if (net?.Camera != null)
                {
                    var camTransform = net.Camera.transform;
                    camTransform.localPosition = Vector3.Lerp(camTransform.localPosition, _originalCamPos, Time.deltaTime * 4f);
                    if (Vector3.Distance(camTransform.localPosition, _originalCamPos) < 0.1f)
                    {
                        camTransform.localPosition = _originalCamPos;
                        _wasThirdPerson = false;
                    }
                }
            }
            catch { }
        }

        // ──── INFINITE BATTERY ────

        private void RefillAllBatteries()
        {
            foreach (var bat in _cachedBatteries)
            {
                try
                {
                    if (bat == null || bat.CanGiveElectricity) continue;
                    bat.AddBatteryUsesNetworked(100);
                }
                catch { }
            }
        }
    }
}
