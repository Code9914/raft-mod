using System;
using UnityEngine;

namespace RaftMod
{
    public class PlayerFeatures
    {
        public bool GodMode;
        public bool InfiniteOxygen;
        public bool InfiniteHunger;
        public bool NoHungerLoss;
        public bool NoClip;
        public float MoveSpeed = 1f;
        public float JumpMultiplier = 1f;
        public float SwimMultiplier = 1f;
        public float Gravity = 20f;
        public bool InfDurability;
        public bool NoFallDamage;
        public bool AutoPickup;
        public float DamageMultiplier = 1f;
        public float FOV = 60f;

        private Network_Player _localPlayer;
        private float _autoPickupTimer;
        private float _autoPickupScanTimer;
        private PickupItem[] _cachedPickups = new PickupItem[0];
        private bool _fovLoaded;
        private float _lastSavedFov;
        private const string FOV_KEY = "RaftMod_FOV";

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

        public float GetHealth()
        {
            try
            {
                var s = LocalPlayer?.Stats;
                return s != null ? s.stat_health.Value : 0f;
            }
            catch { return 0f; }
        }

        public float GetBonusHealth()
        {
            try
            {
                var s = LocalPlayer?.Stats;
                return s != null ? s.stat_BonusHealth.Value : 0f;
            }
            catch { return 0f; }
        }

        public void HealPlayer()
        {
            try
            {
                var net = LocalPlayer;
                if (net?.Stats != null)
                {
                    net.Stats.stat_health.SetToMaxValue();
                    net.Stats.stat_BonusHealth.Value = net.Stats.stat_BonusHealth.Max;
                }
            }
            catch (Exception ex) { Plugin.Log.LogError($"Heal: {ex.Message}"); }
        }

        public void Update()
        {
            try
            {
                if (!_fovLoaded)
                {
                    _fovLoaded = true;
                    FOV = PlayerPrefs.GetFloat(FOV_KEY, 60f);
                    _lastSavedFov = FOV;
                }

                var net = LocalPlayer;
                if (net?.Stats == null) return;
                var pc = net.PersonController;

                if (GodMode)
                {
                    net.Stats.stat_health.SetToMaxValue();
                    net.Stats.stat_BonusHealth.Value = net.Stats.stat_BonusHealth.Max;
                    net.BuffManager.RemoveBuffDuration(BuffType.Poison);
                    net.BuffManager.RemoveBuffDuration(BuffType.Radiation);
                }

                if (InfiniteOxygen)
                    net.Stats.stat_oxygen.SetToMaxValue();

                if (InfiniteHunger)
                {
                    net.Stats.stat_hunger.Normal.SetToMaxValue();
                    net.Stats.stat_thirst.Normal.SetToMaxValue();
                }

                if (NoClip)
                {
                    if (net.flightCamera != null && !net.flightCamera.enabled)
                        net.flightCamera.Enable(true);
                }
                else
                {
                    if (net.flightCamera != null && net.flightCamera.enabled)
                        net.flightCamera.Disable(true);
                }

                var gm = GameModeValueManager.GetCurrentGameModeValue();
                if (gm != null)
                {
                    gm.toolVariables.areToolsIndestructible = InfDurability;
                    gm.playerSpecificVariables.recieveFallDamage = !NoFallDamage;
                    gm.playerSpecificVariables.outgoingDamageMultiplierPVE = DamageMultiplier;
                }

                var settings = ComponentManager<Settings>.Value;
                if (settings?.graphicsBox?.FOVSlider != null)
                    settings.graphicsBox.FOVSlider.value = FOV;

                if (Math.Abs(FOV - _lastSavedFov) > 0.01f)
                {
                    _lastSavedFov = FOV;
                    PlayerPrefs.SetFloat(FOV_KEY, FOV);
                    PlayerPrefs.Save();
                }

                if (AutoPickup)
                    DoAutoPickup(net);
                else if (_cachedPickups.Length > 0)
                {
                    _cachedPickups = new PickupItem[0];
                    _autoPickupScanTimer = 0f;
                }

                if (pc != null)
                {
                    pc.normalSpeed = 3f * MoveSpeed;
                    pc.sprintSpeed = 6f * MoveSpeed;
                    pc.jumpSpeed = 8f * JumpMultiplier;
                    pc.swimSpeed = 2f * SwimMultiplier;
                    pc.gravity = Gravity;

                    if (NoHungerLoss)
                    {
                        pc.GroundVelocityModifier = 1f;
                        pc.WaterVelocityModifier = 1f;
                    }
                }
            }
            catch (Exception ex) { Plugin.Log.LogError($"Player.Update: {ex.Message}"); }
        }

        private void DoAutoPickup(Network_Player net)
        {
            _autoPickupTimer -= Time.deltaTime;
            if (_autoPickupTimer > 0f) return;
            _autoPickupTimer = 0.5f;

            _autoPickupScanTimer -= 0.5f;
            if (_autoPickupScanTimer <= 0f)
            {
                _cachedPickups = UnityEngine.Object.FindObjectsOfType<PickupItem>();
                _autoPickupScanTimer = 2f;
            }

            var pickupScript = net.PickupScript;
            if (pickupScript == null) return;

            Vector3 playerPos = net.transform.position;
            const float radiusSqr = 5f * 5f;

            foreach (var pickup in _cachedPickups)
            {
                if (pickup == null) continue;
                if (!pickup.gameObject.activeInHierarchy) continue;
                if (!pickup.canBePickedUp) continue;
                if ((playerPos - pickup.transform.position).sqrMagnitude > radiusSqr) continue;
                pickupScript.PickupItem(pickup, true, false);
            }
        }
    }
}
