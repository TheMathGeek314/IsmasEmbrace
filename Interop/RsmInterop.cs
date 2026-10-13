using RandoSettingsManager;
using RandoSettingsManager.SettingsManagement;
using RandoSettingsManager.SettingsManagement.Versioning;

namespace IsmasEmbrace {
    internal static class RsmInterop {
        public static void Hook() {
            RandoSettingsManagerMod.Instance.RegisterConnection(new EmbraceSettingsProxy());
        }

        internal class EmbraceSettingsProxy: RandoSettingsProxy<GlobalSettings, string> {
            public override string ModKey => IsmasEmbrace.instance.GetName();

            public override VersioningPolicy<string> VersioningPolicy { get; } = new EqualityVersioningPolicy<string>(IsmasEmbrace.instance.GetVersion());

            public override void ReceiveSettings(GlobalSettings settings) {
                settings ??= new();
                RandoMenuPage.Instance.ResetMenu(settings);
            }

            public override bool TryProvideSettings(out GlobalSettings settings) {
                settings = IsmasEmbrace.globalSettings;
                return settings.Enabled;
            }
        }
    }
}
