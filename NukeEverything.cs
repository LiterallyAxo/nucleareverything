using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NukeEverything
{
    [BepInPlugin("com.axo.nucleareverything", "We give up on subtlety", "1.0.0")]
    public class NukeEverything : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private Harmony h;
        private static System.Type t1;
        private static System.Type t2;
        private static GameObject fx;

        private static readonly string[] slots =
            { "airEffect", "armorEffect", "terrainEffect", "waterSurfaceEffect", "underwaterEffect" };
        private static readonly string[] nukes =
            { "info_nuclearBomb1", "info_CruiseMissile20kt", "info_nuclearBomb1_strategic" };

        private static System.Type pidType;
        private static MethodInfo m1;
        private static MethodInfo m2;

        private void Awake()
        {
            Log = base.Logger;
            h = new Harmony("com.axo.nuclearoptionbutnuke");

            t1 = AccessTools.TypeByName("Missile");
            if (t1 == null) { Log.LogError("nope"); return; }

            t2 = AccessTools.TypeByName("Shockwave");
            if (t2 == null) { Log.LogError("nope2"); return; }

            pidType = AccessTools.TypeByName("PersistentID");
            m1 = t2?.GetMethod("SetOwner",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            m2 = AccessTools.TypeByName("GlobalPosition")?.GetMethod("ToLocalPosition",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            MethodInfo mm = AccessTools.Method(t1, "Awake")
                ?? AccessTools.Method(t1, "Start")
                ?? AccessTools.Method(t1, "OnEnable");
            if (mm == null) { Log.LogError("also nope"); return; }
            h.Patch(mm, postfix: new HarmonyMethod(
                typeof(NukeEverything).GetMethod("a", BindingFlags.Static | BindingFlags.NonPublic)));

            System.Type tt = AccessTools.TypeByName("BulletSim");
            MethodInfo fu = tt != null ? AccessTools.Method(tt, "FixedUpdate") : null;
            if (fu != null)
                h.Patch(fu, postfix: new HarmonyMethod(
                    typeof(NukeEverything).GetMethod("b", BindingFlags.Static | BindingFlags.NonPublic)));
            else
                Log.LogWarning("whatever");

            System.Type tt2 = AccessTools.TypeByName("DamageEffects");
            MethodInfo bf = tt2 != null ? AccessTools.Method(tt2, "BlastFrag") : null;
            if (bf != null)
                h.Patch(bf, postfix: new HarmonyMethod(
                    typeof(NukeEverything).GetMethod("c", BindingFlags.Static | BindingFlags.NonPublic)));
            else
                Log.LogWarning("whatever2");

            if (m1 != null)
                h.Patch(m1, postfix: new HarmonyMethod(
                    typeof(NukeEverything).GetMethod("d", BindingFlags.Static | BindingFlags.NonPublic)));
            else
                Log.LogWarning("whatever3");

            System.Type tt3 = AccessTools.TypeByName("Unit");
            MethodInfo ud = tt3 != null ? AccessTools.Method(tt3, "UnitDisabled") : null;
            if (ud != null)
                h.Patch(ud, postfix: new HarmonyMethod(
                    typeof(NukeEverything).GetMethod("e", BindingFlags.Static | BindingFlags.NonPublic)));
            else
                Log.LogWarning("whatever4");

            System.Type tt4 = AccessTools.TypeByName("Laser");
            MethodInfo lfu = tt4 != null ? AccessTools.Method(tt4, "FixedUpdate") : null;
            if (lfu != null)
                h.Patch(lfu, postfix: new HarmonyMethod(
                    typeof(NukeEverything).GetMethod("f", BindingFlags.Static | BindingFlags.NonPublic)));
            else
                Log.LogWarning("whatever5");

            SceneManager.sceneLoaded += (s, m3) => { g(); hh(); };

            Log.LogInfo("ok");
        }

        private void OnDestroy() => h?.UnpatchSelf();

        private static bool isnuke(string n)
        {
            if (n == null) return false;
            foreach (string s in nukes) if (n == s) return true;
            return false;
        }

        private static object getprop(object obj, string n)
        {
            if (obj == null) return null;
            var p = obj.GetType().GetProperty(n,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null) try { return p.GetValue(obj); } catch { }
            return get(obj, n);
        }

        // bullets i think
        private static void b(object __instance)
        {
            if (fx == null || m2 == null) return;
            if (get(__instance, "visualOnly") is bool v && v) return;
            try
            {
                object list = get(__instance, "bullets");
                if (!(list is System.Collections.IEnumerable items)) return;
                foreach (object item in items)
                {
                    if (item == null) continue;
                    if (!(get(item, "active") is bool active) || active) continue;
                    object gp = get(item, "position");
                    if (gp == null) continue;
                    Vector3 pos = (Vector3)m2.Invoke(gp, null);
                    GameObject go = GameObject.Instantiate(fx, pos, Quaternion.identity);
                    Component sw = go?.GetComponentInChildren(t2);
                    if (sw == null) continue;
                    object pid = System.Activator.CreateInstance(pidType);
                    m1?.Invoke(sw, new object[] { pid, 20f });
                }
            }
            catch (System.Exception ex) { Log.LogError($"b broke: {ex}"); }
        }

        // blast one
        private static void c(float blastYield, Vector3 blastPosition)
        {
            if (fx == null || blastYield <= 0f) return;
            try
            {
                GameObject go = GameObject.Instantiate(fx, blastPosition, Quaternion.identity);
                Component sw = go?.GetComponentInChildren(t2);
                if (sw == null) return;
                object pid = System.Activator.CreateInstance(pidType);
                m1?.Invoke(sw, new object[] { pid, 20f });
            }
            catch (System.Exception ex) { Log.LogError($"c broke: {ex}"); }
        }

        // idk setowner thing
        private static void d(object __instance)
        {
            set(__instance, "yieldKilotons", 20f);
        }

        // death
        private static void e(object __instance, bool nowDisabled)
        {
            if (!nowDisabled || fx == null) return;
            try
            {
                Component c = __instance as Component;
                if (c == null) return;
                float kt = 20f;
                object def = getprop(__instance, "definition");
                if (def != null)
                {
                    string name = get(def, "unitName") as string;
                    if (name != null && name.IndexOf("Cricket", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        kt = 250f;
                }
                Vector3 pos = c.transform.position;
                GameObject go = GameObject.Instantiate(fx, pos, Quaternion.identity);
                Component sw = go?.GetComponentInChildren(t2);
                if (sw == null) return;
                object pid = System.Activator.CreateInstance(pidType);
                m1?.Invoke(sw, new object[] { pid, kt });
            }
            catch (System.Exception ex) { Log.LogError($"e broke: {ex}"); }
        }

        // laser thing
        private static void f(object __instance)
        {
            if (fx == null) return;
            if (!(get(__instance, "fireCommanded") is bool fired) || !fired) return;
            if (!(get(__instance, "lastDamageTick") is float tick) || tick != Time.timeSinceLevelLoad) return;
            try
            {
                object hs = get(__instance, "hitEffectSpawn");
                GameObject hsgo = hs as GameObject;
                if (hsgo == null) return;
                Vector3 pos = hsgo.transform.position;
                GameObject go = GameObject.Instantiate(fx, pos, Quaternion.identity);
                Component sw = go?.GetComponentInChildren(t2);
                if (sw == null) return;
                object pid = System.Activator.CreateInstance(pidType);
                m1?.Invoke(sw, new object[] { pid, 20f });
            }
            catch (System.Exception ex) { Log.LogError($"f broke: {ex}"); }
        }

        // missile spawn patch
        private static void a(object __instance)
        {
            try
            {
                if (__instance?.GetType().Name != "Missile") return;
                object info = get(__instance, "info");
                if (isnuke((info as Object)?.name))
                {
                    set(__instance, "blastYield", 0f);
                    if (info != null) set(info, "nuclear", false);
                    return;
                }
                set(__instance, "blastYield", 20000000f);
                object warhead = get(__instance, "warhead");
                if (warhead != null && fx != null)
                    foreach (string slot in slots)
                        set(warhead, slot, fx);
            }
            catch { }
        }

        // prefabs thing
        private static void g()
        {
            if (fx == null)
                fx = findfx();

            foreach (Object obj in Resources.FindObjectsOfTypeAll(typeof(Object)))
            {
                if (obj == null || obj.GetType().Name != "WeaponInfo") continue;

                bool isNuke = isnuke(obj.name);
                bool isGun = get(obj, "gun") is bool gun && gun;

                FieldInfo pf = obj.GetType().GetField("weaponPrefab",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                GameObject prefab = pf?.GetValue(obj) as GameObject;

                if (isNuke)
                {
                    set(obj, "nuclear", false);
                    if (prefab != null)
                    {
                        Component missile = prefab.GetComponent("Missile");
                        if (missile != null) set(missile, "blastYield", 0f);
                    }
                    continue;
                }

                if (prefab == null) continue;

                Component m = prefab.GetComponent("Missile");
                if (m == null) continue;

                set(m, "blastYield", 20000000f);
                if (fx == null) continue;
                object warhead = get(m, "warhead");
                if (warhead == null) continue;
                foreach (string slot in slots)
                    set(warhead, slot, fx);
            }

            Log.LogInfo($"g done ({(fx != null ? fx.name : "no fx")})");
        }

        // the other one
        private static void hh()
        {
            Component[] all = Resources.FindObjectsOfTypeAll(typeof(Component)) as Component[];
            if (all == null) return;
            int n = 0;
            foreach (Component c in all)
            {
                if (c == null || c.GetType().Name != "Missile") continue;
                if (!c.gameObject.scene.IsValid() || !c.gameObject.activeInHierarchy) continue;
                object info = get(c, "info");
                if (isnuke((info as Object)?.name))
                {
                    set(c, "blastYield", 0f);
                    if (info != null) set(info, "nuclear", false);
                    continue;
                }
                set(c, "blastYield", 20000000f);
                if (fx != null)
                {
                    object warhead = get(c, "warhead");
                    if (warhead != null)
                        foreach (string slot in slots)
                            set(warhead, slot, fx);
                }
                n++;
            }
            Log.LogInfo($"hh done ({n})");
        }

        private static GameObject findfx()
        {
            string[] src = { "info_nuclearBomb1", "info_CruiseMissile20kt", "info_nuclearBomb1_strategic" };
            foreach (Object obj in Resources.FindObjectsOfTypeAll(typeof(Object)))
            {
                if (obj == null || obj.GetType().Name != "WeaponInfo") continue;
                bool match = false;
                foreach (string s in src) if (obj.name == s) { match = true; break; }
                if (!match) continue;

                FieldInfo pf = obj.GetType().GetField("weaponPrefab",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                GameObject prefab = pf?.GetValue(obj) as GameObject;
                if (prefab == null) continue;

                Component missile = prefab.GetComponent("Missile");
                if (missile == null) continue;

                object warhead = get(missile, "warhead");
                if (warhead == null) continue;

                foreach (string slot in slots)
                {
                    GameObject eff = get(warhead, slot) as GameObject;
                    if (eff == null) continue;
                    if (eff.GetComponentInChildren(t2) != null)
                    {
                        Log.LogInfo($"found it: {eff.name}");
                        return eff;
                    }
                }
            }
            return null;
        }

        private static object get(object obj, string f) =>
            obj?.GetType().GetField(f, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(obj);

        private static void set(object obj, string f, object v)
        {
            if (obj == null) return;
            FieldInfo fi = obj.GetType().GetField(f,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi == null) return;
            try { fi.SetValue(obj, v); } catch { }
        }
    }
}