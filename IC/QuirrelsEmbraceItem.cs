using ItemChanger;
using ItemChanger.Tags;
using ItemChanger.UIDefs;

namespace IsmasEmbrace {
    public class QuirrelsEmbraceItem: AbstractItem {
        public QuirrelsEmbraceItem() {
            name = Consts.WaterItem;
            InteropTag tag = RandoInterop.AddTag(this);
            tag.Properties["PinSprite"] = new EmbraceSprite("Walljump");
            tag.Properties["PoolGroup"] = "Skills";
            AddTag(new ItemChainTag {
                predecessor = ItemNames.Swim,
                successor = null
            });
            UIDef = new BigUIDef {
                bigSprite = new ItemChangerSprite("Prompts.Swim"),
                take = new LanguageString("Prompts", "GET_ITEM_INTRO1"),
                press = new LanguageString("Prompts", "BUTTON_DESC_PRESS"),
                buttonSkin = new HeroActionButtonSkin(GlobalEnums.HeroActionButton.DREAM_NAIL),
                descOne = new BoxedString("Take buoyancy into your own hands."),
                descTwo = new BoxedString("Descend beneath the surface of water."),
                name = new BoxedString("Quirrel's Embrace"),
                shopDesc = new BoxedString("Grab your scuba gear, we're going deep diving for diamonds in the depths of danger."),
                sprite = new ItemChangerSprite("ShopIcons.Swim")
            };
        }

        public override void GiveImmediate(GiveInfo info) {
            AcidDivingModule.instance.hasWaterDive = true;
            AcidDivingModule.instance.PrepareScene(null, GameManager.instance);
        }

        public override bool Redundant() {
            return RandomizerMod.RandomizerMod.RS.TrackerData.pm.Get("SWIM") > 1;
        }
    }
}
