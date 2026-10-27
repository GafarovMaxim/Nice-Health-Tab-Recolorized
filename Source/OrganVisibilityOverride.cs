using NiceHealthTab;

namespace NiceHealthTab_YAPEM_Patch
{
    // Lets the original DrawPart draw a damaged organ even if the user hid organs in the overview.
    // Force() is called from a DrawPart prefix, Restore() from its finalizer (runs even on exceptions).
    public static class OrganVisibilityOverride
    {
        private static bool forced;
        private static bool savedShowOrgan;
        private static bool savedShowImplantedOrgans;

        public static void Force()
        {
            if (forced) return;
            savedShowOrgan = NiceHediffTabSettings.ShowOrgan;
            savedShowImplantedOrgans = NiceHediffTabSettings.ShowImplantedOrgans;
            NiceHediffTabSettings.ShowOrgan = true;
            NiceHediffTabSettings.ShowImplantedOrgans = true;
            forced = true;
        }

        public static void Restore()
        {
            if (!forced) return;
            NiceHediffTabSettings.ShowOrgan = savedShowOrgan;
            NiceHediffTabSettings.ShowImplantedOrgans = savedShowImplantedOrgans;
            forced = false;
        }
    }
}
