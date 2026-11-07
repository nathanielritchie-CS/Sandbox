using System.Reflection;
// Reflection helpers: a Player's script eval cannot reference project assemblies at compile time.
Type T(string n) { foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies()) { var t = a.GetType(n); if (t != null) return t; } throw new System.Exception("no type " + n); }
BindingFlags BF() => BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
object Prop(object o, string n) { var t = o as Type ?? o.GetType(); var inst = o is Type ? null : o; var p = t.GetProperty(n, BF()); if (p != null) return p.GetValue(inst); var f = t.GetField(n, BF()); if (f == null) throw new System.Exception("no member " + n + " on " + t.Name); return f.GetValue(inst); }
object Call(object o, string n, params object[] a) { var t = o as Type ?? o.GetType(); var inst = o is Type ? null : o; var m = t.GetMethod(n, BF()); if (m == null) throw new System.Exception("no method " + n + " on " + t.Name); return m.Invoke(inst, a); }
// Zero-argument call that picks the parameterless overload (e.g. UnityEvent.Invoke()).
object Call0(object o, string n) { var t = o as Type ?? o.GetType(); var inst = o is Type ? null : o; var m = t.GetMethod(n, BF(), null, Type.EmptyTypes, null); if (m == null) throw new System.Exception("no method " + n + "() on " + t.Name); return m.Invoke(inst, null); }
// Sets a UI InputField's text.
void SetText(object inputField, string text) { inputField.GetType().GetProperty("text").SetValue(inputField, text); }
// Clicks a UI Button (invokes onClick).
void Click(object button) { Call0(Prop(button, "onClick"), "Invoke"); }
object CallS(Type t, string n, Type[] sig, params object[] a) { var m = t.GetMethod(n, BF(), null, sig, null); if (m == null) throw new System.Exception("no method " + n); return m.Invoke(null, a); }
string AddVirtualInput() {
    var isys = T("UnityEngine.InputSystem.InputSystem");
    var sig = new[] { typeof(string), typeof(string), typeof(string) };
    var kb = CallS(isys, "AddDevice", sig, "Keyboard", null, null);
    var ms = CallS(isys, "AddDevice", sig, "Mouse", null, null);
    return "added " + kb + " + " + ms;
}
UnityEngine.Object First(string typeName) => Resources.FindObjectsOfTypeAll(T(typeName)).FirstOrDefault();
UnityEngine.Object[] Scene(string typeName) => UnityEngine.Object.FindObjectsByType(T(typeName));
object NM() => Prop(T("Unity.Netcode.NetworkManager"), "Singleton");
Component LocalPlayer() { foreach (var o in Scene("Isekai.Net.PlayerMovement")) if ((bool)Prop(o, "IsOwner")) return (Component)o; return null; }
void MoveLocal(Vector3 delta) { LocalPlayer().GetComponent<CharacterController>().Move(delta); }
void FaceLocal(Vector3 dir) { LocalPlayer().transform.rotation = Quaternion.LookRotation(dir); }
UnityEngine.Object LocalAttack() { foreach (var a in Scene("Isekai.Net.PlayerAttack")) if ((bool)Prop(a, "IsOwner")) return a; return null; }
// Stand the local player at (x, z) facing (fx, fz), as one legal-speed step (<= ~5 m).
void StandAt(float x, float z, float fx, float fz) { var p = LocalPlayer().transform.position; MoveLocal(new Vector3(x - p.x, 0f, z - p.z)); FaceLocal(new Vector3(fx, 0f, fz)); }
// Attack until the dummy is dead (or maxShots); returns shots fired.
async Task<int> KillDummy(int maxShots = 25) {
    var atk = LocalAttack(); var dummy = Scene("Isekai.Net.NetworkHealth")[0]; var shots = 0;
    var noTarget = Activator.CreateInstance(T("Unity.Netcode.NetworkObjectReference"));
    while ((int)Prop(dummy, "Current") > 0 && shots < maxShots) { Call(atk, "AttackRpc", noTarget); shots++; await Task.Delay(650); }
    await Task.Delay(500);
    return shots;
}
string Status() {
    var sb = new System.Text.StringBuilder();
    sb.Append(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    var nm = NM();
    if (nm == null) return sb.Append(" nm=null").ToString();
    sb.Append(" local=" + Prop(nm, "LocalClientId"));
    if ((bool)Prop(nm, "IsServer")) sb.Append(" clients=" + ((System.Collections.ICollection)Prop(nm, "ConnectedClientsIds")).Count);
    foreach (var o in Scene("Isekai.Net.PlayerMovement")) {
        var go = ((Component)o).gameObject; var ps = go.GetComponent(T("Isekai.Net.PlayerStats"));
        var hp = go.GetComponent(T("Isekai.Net.NetworkHealth")); var st = go.GetComponent(T("Isekai.Net.PlayerStamina"));
        sb.Append(" | P" + Prop(o, "OwnerClientId") + ((bool)Prop(o, "IsOwner") ? "*" : "") + " " + go.transform.position.ToString("F1") + " L" + Prop(ps, "Level") + " xp" + Prop(Prop(ps, "_xp"), "Value") + (hp != null ? " hp" + Prop(hp, "Current") : "") + (st != null ? " st" + Prop(st, "Current") : ""));
    }
    foreach (var o in Scene("Isekai.Net.NetworkHealth")) if (((Component)o).name == "TrainingDummy") sb.Append(" | dummy " + Prop(o, "Current") + "/" + Prop(o, "Max") + (((Component)o).GetComponent<Renderer>().enabled ? "" : " hidden"));
    return sb.ToString();
}
string Window() {
    var w = Scene("Isekai.UI.StatusWindow").FirstOrDefault();
    if (w == null) return "no status window";
    var panel = (GameObject)Prop(w, "_panel");
    return "window " + (panel.activeSelf ? "OPEN" : "closed") + " [" + ((string)Prop(Prop(w, "_text"), "text")).Replace("\n", "|") + "]";
}
string PartyText() { var p = Scene("Isekai.UI.PartyPanel").FirstOrDefault(); return p == null ? "-" : ((string)Prop(Prop(p, "_text"), "text")).Replace("\n", "|"); }
string Party() {
    var pm = Scene("Isekai.Net.PartyManager").FirstOrDefault();
    if (pm == null) return "no party manager";
    var ids = new List<string>();
    var list = Prop(pm, "_members"); var item = list.GetType().GetProperty("Item");
    for (var i = 0; i < (int)Prop(list, "Count"); i++) ids.Add(item.GetValue(list, new object[] { i }).ToString());
    var inviter = (ulong)Prop(pm, "_inviter");
    return "party[" + string.Join(",", ids) + "]" + (inviter == ulong.MaxValue ? "" : " inviteFrom=" + inviter) + " ui{" + PartyText() + "}";
}
// Host-side stand-in for pressing a key (the Editor ignores simulated keys): "OnInteract" (E) or "OnLeave" (L).
void HostPress(string handler) { var pm = Scene("Isekai.Net.PartyManager").First(); Call(pm, handler, Activator.CreateInstance(T("UnityEngine.InputSystem.InputAction+CallbackContext"))); }
string Enemy() {
    var ai = Scene("Isekai.Net.EnemyAI").FirstOrDefault();
    if (ai == null) return "no enemy";
    var go = ((Component)ai).gameObject; var h = go.GetComponent(T("Isekai.Net.NetworkHealth"));
    return "enemy " + go.transform.position.ToString("F1") + " hp" + Prop(h, "Current") + "/" + Prop(h, "Max") + " " + Anim(go.transform);
}
// HUD health bar: "<label> fill=<0..1>", or "hidden" while there is no local player.
string Health() { var h = Scene("Isekai.UI.HealthBar").FirstOrDefault(b => (UnityEngine.Object)Prop(b, "_label") != null); if (h == null) return "no health bar"; if (!((GameObject)Prop(h, "_panel")).activeSelf) return "hidden"; return (string)Prop(Prop(h, "_label"), "text") + " fill=" + ((RectTransform)Prop(h, "_fill")).anchorMax.x.ToString("F2"); }
string StaminaHud() { var s = Scene("Isekai.UI.HealthBar").FirstOrDefault(b => (UnityEngine.Object)Prop(b, "_label") == null); if (s == null) return "no stamina bar"; if (!((GameObject)Prop(s, "_panel")).activeSelf) return "hidden"; return "stamina fill=" + ((RectTransform)Prop(s, "_fill")).anchorMax.x.ToString("F2"); }
// Text of the HUD label bound to the channel asset with this name (e.g. "SessionStatusChannel").
string Hud(string channelName) { foreach (var l in Scene("Isekai.UI.TextChannelLabel")) if (((UnityEngine.Object)Prop(l, "_channel")).name == channelName) return (string)Prop(Prop(l, "_text"), "text"); return "-"; }
// "<model>:<state>" for a character's CharacterAnimator (state: Loco(<clip>), Attack, Hit, Death, or hidden).
string Anim(Component character) {
    var ca = character.GetComponent(T("Isekai.Net.CharacterAnimator"));
    if (ca == null) return "no CharacterAnimator";
    var a = (Animator)Prop(ca, "_animator");
    if (a == null) return "no animator yet";
    if (!a.isActiveAndEnabled) return a.name + ":hidden";
    var i = a.GetCurrentAnimatorStateInfo(0);
    var named = new[] { "Attack", "Attack2", "Attack3", "Heavy", "Charge", "Roll", "Hit", "Death", "JumpUp", "Fall", "Land" }.FirstOrDefault(n => i.IsName(n));
    var s = i.IsName("Locomotion") ? "Loco(" + string.Join("+", a.GetCurrentAnimatorClipInfo(0).Where(c => c.weight > 0.3f).Select(c => c.clip.name.Split((char)124).Last())) + ")" : named ?? (a.GetNextAnimatorStateInfo(0).IsName("Roll") ? "->Roll" : "?");
    return a.name + ":" + s;
}
Component Player(ulong owner) { foreach (var o in Scene("Isekai.Net.PlayerMovement")) if ((ulong)Prop(o, "OwnerClientId") == owner) return (Component)o; return null; }
Component EnemyComp() => (Component)Scene("Isekai.Net.EnemyAI").FirstOrDefault();
