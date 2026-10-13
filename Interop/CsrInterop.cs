using ConnectionSettingsRando;

namespace IsmasEmbrace {
    internal static class CsrInterop {
        public static void Hook() {
            CSR.Register(
                IsmasEmbrace.instance.GetName(),
                () => IsmasEmbrace.globalSettings,
                s => SettingsRandomizer.CopyTo(s, IsmasEmbrace.globalSettings)
            );
        }
    }
}
