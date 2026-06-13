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

                if (AutoPickup)
                    DoAutoPickup(net);

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

            var pickups = UnityEngine.Object.FindObjectsOfType<PickupItem>();
            var pickupScript = net.PickupScript;
            if (pickupScript == null) return;

            Vector3 playerPos = net.transform.position;
            float radius = 5f;

            foreach (var pickup in pickups)
            {
                if (!pickup.gameObject.activeInHierarchy) continue;
                if (!pickup.canBePickedUp) continue;
                if (Vector3.Distance(playerPos, pickup.transform.position) > radius) continue;
                pickupScript.PickupItem(pickup, true, false);
            }
        }
    }
}
