using MenuChanger;
using MenuChanger.Extensions;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using RandomizerMod.Menu;
using static RandomizerMod.Localization;

namespace IsmasEmbrace {
    public class RandoMenuPage {
        internal MenuPage IeRandoPage;
        internal MenuElementFactory<GlobalSettings> ieMEF;
        internal VerticalItemPanel ieVIP;
        internal SmallButton JumpToIeButton;
        internal static RandoMenuPage Instance { get; private set; }

        public static void Hook() {
            RandomizerMenuAPI.AddMenuPage(ConstructMenu, HandleButton);
            MenuChangerMod.OnExitMainMenu += () => Instance = null;
        }

        private static bool HandleButton(MenuPage landingPage, out SmallButton button) {
            button = Instance.JumpToIeButton;
            return true;
        }

        private void SetTopLevelButtonColor() {
            if(JumpToIeButton != null)
                JumpToIeButton.Text.color = IsmasEmbrace.globalSettings.Enabled ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
        }

        private static void ConstructMenu(MenuPage landingPage) => Instance = new(landingPage);

        private RandoMenuPage(MenuPage landingPage) {
            IeRandoPage = new MenuPage(Localize("Isma's Embrace"), landingPage);
            ieMEF = new(IeRandoPage, IsmasEmbrace.globalSettings);
            ieVIP = new(IeRandoPage, new(0, 300), 75f, true, ieMEF.Elements);
            Localize(ieMEF);
            foreach(IValueElement e in ieMEF.Elements)
                e.SelfChanged += obj => SetTopLevelButtonColor();
            JumpToIeButton = new(landingPage, Localize("Isma's Embrace"));
            JumpToIeButton.AddHideAndShowEvent(landingPage, IeRandoPage);
            SetTopLevelButtonColor();
        }

        internal void ResetMenu(GlobalSettings settings) {
            ieMEF.SetMenuValues(settings);
            SetTopLevelButtonColor();
        }
    }
}
