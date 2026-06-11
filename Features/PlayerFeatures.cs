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
                        net.flightCamera.Disable(false);
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
    }
}
