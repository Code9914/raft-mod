using System;
using System.Linq;
using UnityEngine;

namespace RaftMod
{
    public class RaftFeatures
    {
        public bool InfiniteFuel;
        public bool AnchorAll;
        public float MaxSpeed = 5f;
        public float Acceleration = 2f;

        private Raft _raft;
        private Fuel[] _cachedFuel = new Fuel[0];
        private float _fuelTimer;

        public void Update()
        {
            try
            {
                if (_raft == null)
                    _raft = UnityEngine.Object.FindObjectOfType<Raft>();

                if (_raft != null)
                {
                    _raft.maxVelocity = MaxSpeed;
                    _raft.accelerationSpeed = Acceleration;

                    if (AnchorAll)
                        EngineControls.AnchorsAreDown = true;
                }

                _fuelTimer -= Time.deltaTime;
                if (_fuelTimer <= 0f)
                {
                    if (InfiniteFuel)
                        _cachedFuel = UnityEngine.Object.FindObjectsOfType<Fuel>();
                    _fuelTimer = 1f;
                }

                if (InfiniteFuel)
                    RefillAllFuel();
            }
            catch (Exception ex) { Plugin.Log.LogError($"Raft.Update: {ex.Message}"); }
        }

        public void TeleportToPlayer()
        {
            try
            {
                var net = ComponentManager<Network_Player>.Value;
                if (_raft != null && net != null)
                {
                    var pos = net.transform.position;
                    _raft.transform.position = new Vector3(pos.x, _raft.transform.position.y, pos.z);
                }
            }
            catch (Exception ex) { Plugin.Log.LogError($"TP Raft: {ex.Message}"); }
        }

        private void RefillAllFuel()
        {
            foreach (var fuel in _cachedFuel)
            {
                if (fuel != null && !fuel.HasMaxFuel())
                    fuel.SetFuelCount(fuel.MaxFuel);
            }
        }
    }
}
