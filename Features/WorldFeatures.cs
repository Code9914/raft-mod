using System;
using UnityEngine;

namespace RaftMod
{
    public class WorldFeatures
    {
        public bool NoEnemies;
        public bool NoShark;
        public bool AlwaysDay;
        public float TargetHour = 8f;
        public float TimeSpeed = 1f;

        private AI_NetworkBehaviour[] _cachedEnemies = new AI_NetworkBehaviour[0];
        private AI_NetworkBehavior_Shark _cachedShark;
        private SkyManager _cachedSky;
        private float _cacheTimer;

        public float GetCurrentHour()
        {
            try
            {
                if (_cachedSky == null)
                    _cachedSky = UnityEngine.Object.FindObjectOfType<SkyManager>();
                if (_cachedSky?.skyController != null)
                    return _cachedSky.skyController.timeOfDay.hour;
            }
            catch { }
            return 12f;
        }

        public void SetHour(float hour)
        {
            try
            {
                var sky = _cachedSky ?? UnityEngine.Object.FindObjectOfType<SkyManager>();
                if (sky?.skyController != null)
                    sky.skyController.timeOfDay.GotoTime(hour);
            }
            catch (Exception ex) { Plugin.Log.LogError($"SetHour: {ex.Message}"); }
        }

        public void SetWeather(int type)
        {
            try
            {
                var wm = UnityEngine.Object.FindObjectOfType<WeatherManager>();
                if (wm == null) return;

                switch (type)
                {
                    case 0: wm.SetWeather(UniqueWeatherType.Default, true); break;
                    case 1: wm.SetWeather(UniqueWeatherType.Rain, true); break;
                    case 2: wm.SetWeather("Storm", true); break;
                    case 3: wm.SetWeather(UniqueWeatherType.Fog, true); break;
                    case 4: wm.SetWeather(UniqueWeatherType.Snow, true); break;
                    case 5: wm.SetWeather(UniqueWeatherType.Calm, true); break;
                }
            }
            catch (Exception ex) { Plugin.Log.LogError($"SetWeather: {ex.Message}"); }
        }

        public void KillAllEnemies()
        {
            try
            {
                foreach (var ai in UnityEngine.Object.FindObjectsOfType<AI_NetworkBehaviour>())
                {
                    if (ai is AI_NetworkBehavior_Shark) continue;
                    if (!ai.gameObject.activeSelf) continue;

                    var entity = ai.GetComponent<Network_Entity>();
                    if (entity != null && !entity.IsDead)
                        entity.Damage(99999f, Vector3.zero, Vector3.up, EntityType.Player, null);
                }
            }
            catch (Exception ex) { Plugin.Log.LogError($"KillAll: {ex.Message}"); }
        }

        public void TeleportToRaft()
        {
            try
            {
                var raft = UnityEngine.Object.FindObjectOfType<Raft>();
                var net = ComponentManager<Network_Player>.Value;
                if (raft != null && net != null)
                    net.transform.position = raft.transform.position + Vector3.up * 3f;
            }
            catch (Exception ex) { Plugin.Log.LogError($"TP Raft: {ex.Message}"); }
        }

        public void TeleportToCamera()
        {
            try
            {
                var net = ComponentManager<Network_Player>.Value;
                if (net?.Camera == null) return;

                var cam = net.Camera;
                if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, 1000f))
                    net.transform.position = hit.point + Vector3.up * 1.5f;
            }
            catch (Exception ex) { Plugin.Log.LogError($"TP Camera: {ex.Message}"); }
        }

        public void Update()
        {
            try
            {
                _cacheTimer -= Time.deltaTime;
                if (_cacheTimer <= 0f)
                {
                    if (NoEnemies)
                        _cachedEnemies = UnityEngine.Object.FindObjectsOfType<AI_NetworkBehaviour>();
                    if (NoShark && _cachedShark == null)
                        _cachedShark = UnityEngine.Object.FindObjectOfType<AI_NetworkBehavior_Shark>();
                    if (_cachedSky == null)
                        _cachedSky = UnityEngine.Object.FindObjectOfType<SkyManager>();
                    _cacheTimer = 1f;
                }

                if (NoEnemies) DisableEnemies();
                if (NoShark) DisableShark();
                if (AlwaysDay) ForceDay();

                if (Math.Abs(TimeSpeed - 1f) > 0.01f)
                    ApplyTimeSpeed();
            }
            catch (Exception ex) { Plugin.Log.LogError($"World.Update: {ex.Message}"); }
        }

        private void DisableEnemies()
        {
            foreach (var ai in _cachedEnemies)
            {
                if (ai == null || ai is AI_NetworkBehavior_Shark || !ai.gameObject.activeSelf)
                    continue;
                var entity = ai.GetComponent<Network_Entity>();
                if (entity != null && !entity.IsDead)
                    entity.Damage(99999f, Vector3.zero, Vector3.up, EntityType.Player, null);
            }
        }

        private void DisableShark()
        {
            if (_cachedShark == null || !_cachedShark.gameObject.activeSelf) return;
            var entity = _cachedShark.GetComponent<Network_Entity>();
            if (entity != null && !entity.IsDead)
                entity.Damage(99999f, Vector3.zero, Vector3.up, EntityType.Player, null);
        }

        private void ForceDay()
        {
            if (_cachedSky?.skyController != null)
            {
                var hour = _cachedSky.skyController.timeOfDay.hour;
                if (hour < 6f || hour > 18f)
                    _cachedSky.skyController.timeOfDay.GotoTime(8f);
            }
        }

        private void ApplyTimeSpeed()
        {
            if (_cachedSky?.skyController != null)
            {
                var cur = _cachedSky.skyController.timeOfDay.hour;
                _cachedSky.skyController.timeOfDay.GotoTime(cur + (TimeSpeed - 1f) * Time.deltaTime * 0.5f);
            }
        }
    }
}
