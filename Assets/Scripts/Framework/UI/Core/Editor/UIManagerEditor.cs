using UnityEngine.Scripting.APIUpdating;

namespace AXR.Framework.UI
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(UIManager))]
    public class UIManagerEditor : Editor
    {
        private int selectedEntryIndex;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawRuntimeDebugTools((UIManager)target);
        }

        private void DrawRuntimeDebugTools(UIManager manager)
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Runtime Debug", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后可操作", MessageType.Info);
                return;
            }

            IReadOnlyList<UIPrefabEntry> entries = manager.RegisteredEntries;
            List<int> validCatalogIndices = new();
            List<string> options = new();

            for (int i = 0; i < entries.Count; i++)
            {
                UIPrefabEntry entry = entries[i];
                if (entry == null || entry.prefab == null)
                {
                    continue;
                }

                UIPageBase page = entry.prefab.GetComponent<UIPageBase>();
                if (page == null)
                {
                    continue;
                }

                validCatalogIndices.Add(i);
                options.Add($"{page.GetType().Name} / {entry.layerType}");

            }

            if (options.Count == 0)
            {
                EditorGUILayout.HelpBox("当前 catalog 中没有可打开的 UIPageBase 页面。", MessageType.Warning);
                return;
            }

            selectedEntryIndex = Mathf.Clamp(selectedEntryIndex, 0, options.Count - 1);
            selectedEntryIndex = EditorGUILayout.Popup("Page", selectedEntryIndex, options.ToArray());

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Selected Page"))
                {
                    manager.OpenPageByCatalogIndex(validCatalogIndices[selectedEntryIndex]);
                }

                if (GUILayout.Button("Close Top Page"))
                {
                    manager.CloseTopPage();
                }
            }
        }
    }
#endif
}
