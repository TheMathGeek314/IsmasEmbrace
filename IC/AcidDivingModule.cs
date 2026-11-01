using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using GlobalEnums;
using UnityEngine;
using UnityEngine.SceneManagement;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using ItemChanger.Modules;
using RandomizerCore.Logic;

namespace IsmasEmbrace {
    internal class AcidDivingModule: ItemChanger.Modules.Module {
        public static AcidDivingModule instance;
        public bool hasAcidDive;
        public bool hasWaterDive;

        private bool canDiveHere = false;
        private bool isDiving = false;
        private bool isInteracting = false;

        private FieldInfo _inputHandler = typeof(HeroController).GetField("inputHandler", BindingFlags.NonPublic | BindingFlags.Instance);
        private PropertyInfo _fallTimer = typeof(HeroController).GetProperty("fallTimer", BindingFlags.Public | BindingFlags.Instance);
        private MethodInfo _play = typeof(HeroAnimationController).GetMethod("Play", BindingFlags.NonPublic | BindingFlags.Instance);
        
        public override void Initialize() {
            instance = this;
            using Stream s = typeof(AcidDivingModule).Assembly.GetManifestResourceStream("IsmasEmbrace.Resources.rooms.json");
            foreach(SwimRoom data in new ParseJson(s).ParseFile<SwimRoom>()) {
                data.translate();
            }
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += EarlyPrepareScene;
            On.GameManager.OnNextLevelReady += PrepareScene;
            On.PlayMakerFSM.OnEnable += EditKnightSwim;
            On.HeroController.CanInspect += OverrideInspect;
            On.HeroController.RegainControl += OverrideRegainControl;
            On.HeroController.TakeDamage += OverrideTakeDamage;
            
            if(ItemChangerMod.Modules?.Get<InventoryTracker>() is InventoryTracker it) {
                it.OnGenerateFocusDesc += AddInvTracker;
            }
        }

        public override void Unload() {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= EarlyPrepareScene;
            On.GameManager.OnNextLevelReady -= PrepareScene;
            On.PlayMakerFSM.OnEnable -= EditKnightSwim;
            On.HeroController.CanInspect -= OverrideInspect;
            On.HeroController.RegainControl -= OverrideRegainControl;
            On.HeroController.TakeDamage -= OverrideTakeDamage;

            if(ItemChangerMod.Modules?.Get<InventoryTracker>() is InventoryTracker it) {
                it.OnGenerateFocusDesc -= AddInvTracker;
            }
        }

        private void EditKnightSwim(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self) {
            orig(self);
            InputHandler inputHandler = null;
            if(self.gameObject.name == "Knight" && self.FsmName == "Surface Water") {
                GameObject effects = HeroController.instance.gameObject.FindChild("Effects");
                if(effects.FindChild("Acid Embrace") == null) {
                    GameObject embrace = new("Acid Embrace");
                    embrace.transform.parent = effects.transform;
                    embrace.transform.localPosition = Vector3.zero;
                    GameObject armour = effects.FindChild("Acid Armour");
                    GameObject.Instantiate(armour, effects.transform.position + new Vector3(0f, -0.425f, 0), Quaternion.Euler(0, 0, 120), embrace.transform).SetActive(true);
                    GameObject.Instantiate(armour, effects.transform.position + new Vector3(0f, -0.425f, 0), Quaternion.Euler(0, 0, 240), embrace.transform).SetActive(true);

                    FsmFloat surface = AddFsmFloat(self, "Surface", 0f);
                    FsmString conditionalSwimEvent = AddFsmString(self, "Conditional Swim Event", "");
                    FsmFloat swimX = AddFsmFloat(self, "Swimmy X", 0);
                    FsmFloat swimY = AddFsmFloat(self, "Swimmy Y", 0);
                    FsmState startDiving = self.AddState("Start Diving");
                    FsmState cancelDiving = self.AddState("Cancel Diving");
                    FsmState endDiving = self.AddState("End Diving");
                    foreach(string state in new string[] { "Start Swimming?", "Swim Right", "Swim Left", "Idle" }) {
                        FsmState iState = self.GetState(state);
                        iState.AddTransition("ACIDDIVE", startDiving);
                        iState.AddLastAction(new ListenForDreamNail {
                            eventTarget = new() { target = FsmEventTarget.EventTarget.Self },
                            isPressed = FsmEvent.GetFsmEvent("ACIDDIVE")
                        });
                    }
                    startDiving.AddTransition("CANCEL", cancelDiving);
                    startDiving.AddTransition("SURFACE", endDiving);
                    foreach((string state, string onEvent) in new (string, string)[] {
                        ("Swim Right", "RIGHT"),
                        ("Swim Left", "LEFT"),
                        ("Idle", "IDLE")
                    }) {
                        cancelDiving.AddTransition(onEvent, state);
                    }
                    endDiving.AddTransition("FINISHED", "Idle");
                    startDiving.Actions = [
                        new Lambda(() => {
                            if(!canDiveHere)
                                self.SendEvent("CANCEL");
                            else {
                                Vector3 pos = HeroController.instance.transform.position;
                                surface.Value = pos.y;
                                self.FsmVariables.GetFsmString("Idle Anim").Value = "Surface Idle";
                                inputHandler ??= _inputHandler.GetValue(HeroController.instance) as InputHandler;
                                float x = GameManager.instance.sceneName == "Fungus2_19" ? Mathf.Min(pos.x, 56.7f) : pos.x;
                                float y = pos.y - (GameManager.instance.sceneName.StartsWith("Abyss") ? 0.5f : 1);
                                HeroController.instance.transform.position = new Vector3(x, y, pos.z);
                                HeroController.instance.AffectedByGravity(false);
                                isDiving = true;
                            }
                        }),
                        new Tk2dPlayAnimationWithEvents {
                            gameObject = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.UseOwner },
                            clipName = self.FsmVariables.GetFsmString("Swim Anim")
                        },
                        new FadeAudio {
                            gameObject = new FsmOwnerDefault {
                                OwnerOption = OwnerDefaultOption.SpecifyGameObject,
                                GameObject = self.FsmVariables.GetFsmGameObject("Swim Audio")
                            },
                            startVolume = 0.25f,
                            endVolume = 1f,
                            time = 0.5f
                        },
                        new Lambda(() => {
                            if(SwimRoom.ByScene.TryGetValue(GameManager.instance.sceneName, out SwimRoom roomData) && roomData.type == "Acid" && hasAcidDive)
                                conditionalSwimEvent.Value = "ACID ARMOUR START";
                            else
                                conditionalSwimEvent.Value = "FINISHED";//valid existing event but don't do anything
                        }),
                        new SendEventByName {
                            eventTarget = new() {
                                target = FsmEventTarget.EventTarget.GameObject,
                                gameObject = new() {
                                    OwnerOption = OwnerDefaultOption.SpecifyGameObject,
                                    GameObject = HeroController.instance.gameObject.FindChild(["Effects", "Acid Embrace"])
                                },
                                sendToChildren = true
                            },
                            sendEvent = conditionalSwimEvent,
                            delay = 0,
                            everyFrame = false
                        },
                        new LambdaEveryFrame(() => {
                            float x = 0;
                            float y = 0;
                            float swimSpeedF = self.FsmVariables.GetFsmFloat("Swim Speed").Value;
                            HeroController hero = HeroController.instance;
                            if(!isInteracting) {
                                if(inputHandler.inputActions.up.IsPressed)
                                    y += swimSpeedF;
                                if(inputHandler.inputActions.down.IsPressed)
                                    y -= swimSpeedF;
                                if(inputHandler.inputActions.left.IsPressed)
                                    x -= swimSpeedF;
                                if(inputHandler.inputActions.right.IsPressed)
                                    x += swimSpeedF;
                                if(x > 0.01 && !hero.cState.facingRight)
                                    hero.FaceRight();
                                if(x < -0.01 && hero.cState.facingRight)
                                    hero.FaceLeft();
                            }
                            swimX.Value = x;
                            swimY.Value = y;
                            if(hero.transform.position.y > surface.Value + 0.01f) {
                                Vector3 hp = hero.transform.position;
                                hero.transform.position = new Vector3(hp.x, surface.Value, hp.z);
                                self.SendEvent("SURFACE");
                            }
                            HeroController.instance.cState.willHardLand = false;
                            _fallTimer.SetValue(HeroController.instance, 0);
                        }),
                        new SetVelocity2d {
                            gameObject = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.UseOwner },
                            vector = Vector2.zero,
                            x = swimX,
                            y = swimY,
                            everyFrame = true
                        }
                    ];
                    cancelDiving.Actions = [
                        new ListenForRight {
                            eventTarget = new() { target = FsmEventTarget.EventTarget.Self },
                            isPressed = FsmEvent.GetFsmEvent("RIGHT")
                        },
                        new ListenForLeft {
                            eventTarget = new() { target = FsmEventTarget.EventTarget.Self },
                            isPressed = FsmEvent.GetFsmEvent("LEFT")
                        },
                        new NextFrameEvent {
                            sendEvent = FsmEvent.GetFsmEvent("IDLE")
                        }
                    ];
                    endDiving.Actions = [
                        new SendEventByName {
                            eventTarget = new() {
                                target = FsmEventTarget.EventTarget.GameObject,
                                gameObject = new() {
                                    OwnerOption = OwnerDefaultOption.SpecifyGameObject,
                                    GameObject = HeroController.instance.gameObject.FindChild(["Effects", "Acid Embrace"])
                                },
                                sendToChildren = true
                            },
                            sendEvent = "ACID ARMOUR END",
                            delay = 0,
                            everyFrame = false
                        },
                        new Lambda(() => isDiving = false)
                    ];
                }
            }
            else if(self.FsmName == "Surface Water Region") {
                try {
                    self.GetState("In").GetActionsOfType<SendEventByName>().First(a => a.sendEvent.Value == "ACID ARMOUR START").eventTarget = new() {
                        target = FsmEventTarget.EventTarget.GameObject,
                        gameObject = new() {
                            OwnerOption = OwnerDefaultOption.SpecifyGameObject,
                            GameObject = HeroController.instance.gameObject.FindChild(["Effects", "Acid Armour"])
                        }
                    };
                    self.GetState("Out").GetActionsOfType<SendEventByName>().First(a => a.sendEvent.Value == "ACID ARMOUR END").eventTarget = new() {
                        target = FsmEventTarget.EventTarget.GameObject,
                        gameObject = new() {
                            OwnerOption = OwnerDefaultOption.SpecifyGameObject,
                            GameObject = HeroController.instance.gameObject
                        },
                        sendToChildren = true
                    };
                }
                catch(InvalidOperationException) { }
            }
            else if(self.FsmName == "inspect") {
                self.GetState("Can Inspect?").AddFirstAction(new Lambda(() => isInteracting = isDiving));
                self.GetState("Check Proximity").AddFirstAction(new Lambda(() => {
                    if(isInteracting) {
                        self.SendEvent("FINISHED");
                    }
                }));
            }
        }

        internal void PrepareScene(On.GameManager.orig_OnNextLevelReady orig, GameManager self) {
            orig?.Invoke(self);
            if(Consts.ComplexAcidScenes.TryGetValue(self.sceneName, out string[] objectNames) && hasAcidDive) {
                foreach(GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) {
                    foreach(Transform t in root.GetComponentsInChildren<Transform>(true)) {
                        if(objectNames.Any(name => t.name.StartsWith(name))) {
                            t.GetComponent<SpriteRenderer>().enabled = false;
                        }
                    }
                }
            }
            if(SwimRoom.ByScene.TryGetValue(self.sceneName, out SwimRoom data)) {
                if(data.type == "Acid" && !hasAcidDive) {
                    canDiveHere = false;
                    return;
                }
                if(data.type == "Water" && !hasWaterDive) {
                    canDiveHere = false;
                    return;
                }
                bool equippedHeart = PlayerData.instance.GetInt(nameof(PlayerData.royalCharmState)) >= 4 && PlayerData.instance.GetBool(nameof(PlayerData.equippedCharm_36));
                bool litLighthouse = GameManager.instance.sceneName == "Abyss_09" && PlayerData.instance.GetBool(nameof(PlayerData.abyssLighthouse));
                bool validVoidSwim = hasWaterDive && (litLighthouse|| equippedHeart);
                if(data.type == "Void" && !validVoidSwim) {
                    canDiveHere = false;
                    return;
                }
                canDiveHere = true;
                foreach(string r in data.renderers ?? []) {
                    ((GameObject)(r.Contains("/") switch {
                        false => GameObject.Find(r),
                        true => GameObject.Find(r.Split('/')[0]).FindChild(r.Split('/').Skip(1))
                    })).GetComponent<SpriteRenderer>().enabled = false;
                }
                foreach(CameraLockJson c in data.zones ?? []) {
                    void AssignUnlessDefault(ref float target, float value) {
                        if(value != default)
                            target = value;
                    }
                    GameObject sourceGO = GameObject.Find(c.AreaToClone);
                    CameraLockArea sourceCLA = sourceGO.GetComponent<CameraLockArea>();
                    Vector3 position = new Vector3(c.PosX == default ? sourceGO.transform.position.x : c.PosX, c.PosY == default ? sourceGO.transform.position.y : c.PosY, sourceGO.transform.position.z);
                    Vector3 scale = new Vector3(c.ScaleX == default ? sourceGO.transform.localScale.x : c.ScaleX, c.ScaleY == default ? sourceGO.transform.localScale.y : c.ScaleY, sourceGO.transform.localScale.z);
                    GameObject copy = GameObject.Instantiate(sourceGO, position, Quaternion.identity, sourceGO.transform.parent);
                    copy.transform.localScale = scale;
                    CameraLockArea copyCLA = copy.GetComponent<CameraLockArea>();
                    AssignUnlessDefault(ref copyCLA.cameraXMin, c.MinX);
                    AssignUnlessDefault(ref copyCLA.cameraXMax, c.MaxX);
                    AssignUnlessDefault(ref copyCLA.cameraYMin, c.MinY);
                    AssignUnlessDefault(ref copyCLA.cameraYMax, c.MaxY);
                    copy.SetActive(true);
                }
                foreach(AcidEdit edit in data.acidEdits ?? []) {
                    Transform box = edit.name.Contains("/") switch {
                        true => GameObject.Find(edit.name.Split('/')[0]).FindChild(edit.name.Split('/').Skip(1)).transform,
                        false => GameObject.Find(edit.name).transform
                    };
                    box.position = new Vector3(box.position.x, edit.position, box.position.z);
                    BoxCollider2D col = box.GetComponent<BoxCollider2D>();
                    col.size = new Vector2(col.size.x, edit.size);
                }
                if(data.type == "Void") {
                    foreach(string parent in data.parentObjs ?? []) {
                        foreach(Transform child in GameObject.Find(parent).transform) {
                            if(child.name.Contains("top")) {
                                Vector3 s = child.localScale;
                                child.localScale = new Vector3(s.x, 0.05f, s.z);
                            }
                            else if(child.name.Contains("black_solid")) {
                                child.gameObject.GetComponent<SpriteRenderer>().enabled = false;
                            }
                        }
                    }
                }
            }
            else {
                canDiveHere = false;
            }
            if(self.sceneName == "Abyss_09") {
                BoxCollider2D box = GameObject.Find("Surface Water Region").GetComponent<BoxCollider2D>();
                box.size = new Vector2(162.5812f, 6f);
                box.offset = new Vector2(-16.6142f, -2.7963f);
            }
            if(self.sceneName == "Fungus1_03") {
                GameObject g = GameObject.Find("Surface Water Region");
                g.GetComponent<BoxCollider2D>().offset = new Vector2(-6.0697f, -7);
                g.SetActive(false);
                g.SetActive(true);
            }
            if(self.sceneName == "Fungus2_06") {
                GameObject g = GameObject.Find("Acid Control v2 (4)").FindChild("Surface Water Region");
                g.GetComponent<BoxCollider2D>().enabled = false;
                PolygonCollider2D pc2d = g.AddComponent<PolygonCollider2D>();
                pc2d.SetPath(0, new Vector2[] { new(-56f, -3.44f), new(85f, -3.44f), new(85f, -15), new(50, -15), new(50, -7), new(-56, -7) });
                pc2d.isTrigger = true;
            }
            if(self.sceneName == "Fungus2_09") {
                GameObject g = GameObject.Find("Surface Water Region");
                g.transform.position = new Vector3(14.95f, 9.84f, -2.38f);
                g.GetComponent<BoxCollider2D>().size = new Vector2(333f, 6.5f);
            }
            if(self.sceneName == "Fungus2_20") {
                GameObject.Find("Acid Control v2").FindChild("Surface Water Region").GetComponent<BoxCollider2D>().size = new Vector2(100f, 4.2954f);
            }
            if(self.sceneName == "Fungus2_30") {
                GameObject.Find("Secret Mask").transform.position = new Vector3(69f, 21f, -5.44f);
            }
            if(self.sceneName == "GG_Pipeway") {
                PolygonCollider2D collider = GameObject.Find("Roof Collider (6)").GetComponent<PolygonCollider2D>();
                Vector2[] points = [.. collider.points];
                points[3] = new Vector2(6f, -4f);
                collider.points = points;
            }
            if(self.sceneName == "GG_Waterways") {
                BoxCollider2D box = GameObject.Find("Surface Water Region").GetComponent<BoxCollider2D>();
                box.size = new Vector2(162.5812f, 4);
                box.offset = new Vector2(-14.6142f, -1.8f);
            }
        }

        private void EarlyPrepareScene(Scene arg0, Scene arg1) {
            isDiving = false;
            isInteracting = false;
            if(SwimRoom.ByScene.TryGetValue(arg1.name, out SwimRoom data)) {
                foreach(ColliderJson box in data.boxes ?? []) {
                    GameObject go = new();
                    go.transform.position = new Vector3(box.X, box.Y, 0);
                    go.layer = LayerMask.NameToLayer("Terrain");
                    go.AddComponent<BoxCollider2D>().size = new Vector2(box.SizeX, box.SizeY);
                    go.SetActive(true);
                }
            }
        }

        private bool OverrideInspect(On.HeroController.orig_CanInspect orig, HeroController self) {
            return isInteracting || orig(self);
        }

        private void OverrideRegainControl(On.HeroController.orig_RegainControl orig, HeroController self) {
            if(!isInteracting)
                orig(self);
            else {
                GameManager.instance.StartCoroutine(ResumeSwimming());
                isInteracting = false;
            }
        }

        private void OverrideTakeDamage(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go, CollisionSide damageSide, int damageAmount, int hazardType) {
            if(!isDiving)
                orig(self, go, damageSide, damageAmount, hazardType);
        }

        private IEnumerator ResumeSwimming() {
            HeroController hero = HeroController.instance;
            for(int i = 0; i < 10; i++)
                yield return null;
            hero.hero_state = ActorStates.airborne;
            HeroAnimationController animator = hero.GetComponent<HeroAnimationController>();
            animator.StopControl();
            _play.Invoke(animator, ["Surface Swim"]);
        }

        private void AddInvTracker(StringBuilder sb) {
            ProgressionManager pm = RandomizerMod.RandomizerMod.RS.TrackerData.pm;
            bool acid = pm.Get("ACID") > 1;
            bool water = pm.Get("SWIM") > 1;
            if(acid || water)
                sb.AppendLine($"You can swim beneath the surface of {(acid ? "acid" : "water") + (acid && water ? " and water" : "")}.");
        }

        private static FsmFloat AddFsmFloat(PlayMakerFSM fsm, string name, float value) {
            FsmFloat fsmFloat = new FsmFloat { Name = name, Value = value };
            FsmFloat[] array = new FsmFloat[fsm.FsmVariables.FloatVariables.Length + 1];
            fsm.FsmVariables.FloatVariables.CopyTo(array, 0);
            array[array.Length - 1] = fsmFloat;
            fsm.FsmVariables.FloatVariables = array;
            return fsmFloat;
        }

        private static FsmString AddFsmString(PlayMakerFSM fsm, string name, string value) {
            FsmString fsmString = new FsmString { Name = name, Value = value };
            FsmString[] array = new FsmString[fsm.FsmVariables.StringVariables.Length + 1];
            fsm.FsmVariables.StringVariables.CopyTo(array, 0);
            array[array.Length - 1] = fsmString;
            fsm.FsmVariables.StringVariables = array;
            return fsmString;
        }
    }
}
