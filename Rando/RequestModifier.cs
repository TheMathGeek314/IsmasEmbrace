using System;
using ItemChanger;
using ItemChanger.Tags;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;

namespace IsmasEmbrace {
    public class RequestModifier {
        public static void Hook() {
            RequestBuilder.OnUpdate.Subscribe(-100, ApplyEmbraceDefs);
            RequestBuilder.OnUpdate.Subscribe(-499, SetupItems);
            RequestBuilder.OnUpdate.Subscribe(0, ProgressifySwims);
            RequestBuilder.OnUpdate.Subscribe(-499.5f, DefinePools);
            RequestBuilder.OnUpdate.Subscribe(0, VerifySettings);
        }

        private static void ApplyEmbraceDefs(RequestBuilder rb) {
            if(!IsmasEmbrace.globalSettings.Enabled)
                return;
            foreach(EmbraceData data in EmbraceData.AllLocations) {
                rb.AddLocationByName(data.name);
                rb.EditLocationRequest(data.name, info => {
                    info.customPlacementFetch = (factory, placement) => {
                        if(factory.TryFetchPlacement(data.name, out AbstractPlacement ap))
                            return ap;
                        AbstractLocation absLoc = Finder.GetLocation(data.name);
                        absLoc.flingType = FlingType.DirectDeposit;
                        AbstractPlacement absPla = absLoc.Wrap();
                        factory.AddPlacement(absPla);
                        return absPla;
                    };
                    info.getLocationDef = () => new() {
                        Name = data.name,
                        FlexibleCount = false,
                        AdditionalProgressionPenalty = false,
                        SceneName = data.scene
                    };
                });
            }
        }

        private static void SetupItems(RequestBuilder rb) {
            if(!IsmasEmbrace.globalSettings.Enabled)
                return;
            foreach((string item, string pool) in new (string, string)[] {
                (Consts.AcidItem, PoolNames.Skill),
                (Consts.WaterItem, PoolNames.Swim)
            }) {
                rb.EditItemRequest(item, info => {
                    info.getItemDef = () => new() {
                        Name = item,
                        Pool = pool,
                        MajorItem = false,
                        PriceCap = 500
                    };
                });
                rb.AddItemByName(item);
            }
        }

        private static void ProgressifySwims(RequestBuilder rb) {
            if(!IsmasEmbrace.globalSettings.Enabled)
                return;
            (string, string)[] itemNames = [
                (ItemNames.Ismas_Tear, Consts.AcidItem),
                (ItemNames.Swim, Consts.WaterItem)
            ];
            foreach((string vanilla, string embrace) in itemNames) {
                AbstractItem currentDef = Finder.GetItemInternal(vanilla);
                Finder.GetItemOverride += args => {
                    if(args.ItemName == vanilla) {
                        args.Current = Finder.GetItemInternal(vanilla);
                        ItemChainTag ict = new() { predecessor = null, successor = embrace };
                        if(args.Current.tags == null) {
                            args.Current.tags = [ict];
                        }
                        else if(args.Current.GetTag(out ItemChainTag t)) {
                            t.successor = embrace;
                        }
                        else {
                            args.Current.AddTag(ict);
                        }
                    }
                };
            }
        }

        private static void DefinePools(RequestBuilder rb) {
            GlobalSettings gs = IsmasEmbrace.globalSettings;
            if(!gs.Enabled)
                return;
            if(rb.gs.SplitGroupSettings.RandomizeOnStart) {
                if(gs.Group >= 0 && gs.Group <= 2)
                    gs.Group = rb.rng.Next(3);
            }
            ItemGroupBuilder myGroup = null;
            if(gs.Group > 0) {
                string label = RBConsts.SplitGroupPrefix + gs.Group;
                foreach(ItemGroupBuilder igb in rb.EnumerateItemGroups()) {
                    if(igb.label == label) {
                        myGroup = igb;
                        break;
                    }
                }
                myGroup ??= rb.MainItemStage.AddItemGroup(label);
            }

            rb.OnGetGroupFor.Subscribe(0.01f, ResolveEmbraceGroup);
            bool ResolveEmbraceGroup(RequestBuilder rb, string item, RequestBuilder.ElementType type, out GroupBuilder gb) {
                if(type == RequestBuilder.ElementType.Item) {
                    if(item == Consts.AcidItem) {
                        gb = rb.GetGroupFor(ItemNames.Ismas_Tear);
                        return true;
                    }
                    if(item == Consts.WaterItem) {
                        gb = rb.GetGroupFor(ItemNames.Swim);
                        return true;
                    }
                }
                if(type == RequestBuilder.ElementType.Location) {
                    if(item.StartsWith("Acid_Dive-") || item.StartsWith("Water_Dive-")) {
                        gb = myGroup;
                        return true;
                    }
                }
                gb = default;
                return false;
            }
        }

        private static void VerifySettings(RequestBuilder rb) {
            if(!IsmasEmbrace.globalSettings.Enabled)
                return;
            if(!rb.gs.PoolSettings.Skills || !rb.gs.NoveltySettings.RandomizeSwim)
                throw new SwimSettingsError();

        }
    }

    public class SwimSettingsError: Exception {
        public override string ToString() => "Isma's Embrace requires both Skills and Swim to be randomized.";
    }
}
