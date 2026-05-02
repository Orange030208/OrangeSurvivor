using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
    [MovedFrom("")]
    public static class UIFrameworkConstants
    {
        public const string MENU_ROOT = "Survivors/Systems/UI/";
        public const string SETTINGS_MENU_PATH = MENU_ROOT + "Framework Settings";
        public const string CATALOG_MENU_PATH = MENU_ROOT + "Prefab Catalog";
        public const string MOTION_DEFINITION_MENU_PATH = MENU_ROOT + "Motion Definition";
        public const string DEFAULT_INSTANCE_ID_PREFIX = "UIInst_";
        public const int LAYER_BACKGROUND_SORTING_ORDER = -300;
        public const int LAYER_SCENE_OVERLAY_SORTING_ORDER = -100;
        public const int LAYER_DEFAULT_SORTING_ORDER = 0;
        public const int LAYER_POPUP_SORTING_ORDER = 200;
        public const int LAYER_SYSTEM_SORTING_ORDER = 400;
        public const int LAYER_DEBUG_SORTING_ORDER = 800;
    }
}
