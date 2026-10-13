using ItemChanger;
using ItemChanger.Tags;
using ItemChanger.UIDefs;

namespace IsmasEmbrace {
    public class IsmasEmbraceItem: AbstractItem {
        public IsmasEmbraceItem() {
            name = Consts.AcidItem;
            InteropTag tag = RandoInterop.AddTag(this);
            tag.Properties["PinSprite"] = new EmbraceSprite("Walljump");
            AddTag(new ItemChainTag {
                predecessor = ItemNames.Ismas_Tear,
                successor = null
            });
            UIDef = new BigUIDef {
                bigSprite = new ItemChangerSprite("Prompts.Isma"),
                take = new LanguageString("Prompts", "GET_ITEM_INTRO8"),
                press = new LanguageString("Prompts", "BUTTON_DESC_PRESS"),
                buttonSkin = new HeroActionButtonSkin(GlobalEnums.HeroActionButton.DREAM_NAIL),
                descOne = new LanguageString("Prompts", "GET_ACIDARMOUR_1"),
                descTwo = new BoxedString("Safely dive beneath the surface of acid."),
                name = new BoxedString("Isma's Embrace"),
                shopDesc = new BoxedString("Grab your scuba gear, we're going deep diving for diamonds in the depths of danger."),
                sprite = new ItemChangerSprite("ShopIcons.Isma")
            };
        }

        public override void GiveImmediate(GiveInfo info) {
            AcidDivingModule.instance.hasAcidDive = true;
            AcidDivingModule.instance.PrepareScene(null, GameManager.instance);
            //does this need to be on Quirrel too?
        }

        public override bool Redundant() {
            return RandomizerMod.RandomizerMod.RS.TrackerData.pm.Get("ACID") > 1;
        }
    }
}
