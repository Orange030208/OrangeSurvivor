using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIMotionDefinition))]
public sealed class UIMotionDefinitionEditor : Editor
{
    private static readonly (string Label, Type TrackType)[] TrackTypes =
    {
        ("Alpha", typeof(UIAlphaMotionTrack)),
        ("Move", typeof(UIMoveMotionTrack)),
        ("Sidebar", typeof(UISidebarMotionTrack)),
        ("Scale", typeof(UIScaleMotionTrack)),
        ("Rotate", typeof(UIRotateMotionTrack)),
        ("Graphic Color", typeof(UIGraphicColorMotionTrack)),
        ("Image Fill", typeof(UIImageFillMotionTrack)),
        ("Sprite Swap", typeof(UISpriteSwapMotionTrack)),
        ("Sprite Sequence", typeof(UISpriteSequenceMotionTrack)),
        ("TMP Typewriter", typeof(UITMPTypewriterMotionTrack)),
        ("Material Float", typeof(UIMaterialFloatMotionTrack)),
        ("Material Color", typeof(UIMaterialColorMotionTrack)),
        ("Callback", typeof(UICallbackMotionTrack))
    };

    private SerializedProperty clipsProperty;
    private SerializedProperty useUnscaledTimeProperty;

    private void OnEnable()
    {
        useUnscaledTimeProperty = serializedObject.FindProperty("useUnscaledTime");
        clipsProperty = serializedObject.FindProperty("clips");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(useUnscaledTimeProperty);
        EditorGUILayout.Space();

        DrawClipList();
        EditorGUILayout.Space();
        DrawAddClipButtons();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawClipList()
    {
        for (int i = 0; i < clipsProperty.arraySize; i++)
        {
            SerializedProperty clipProperty = clipsProperty.GetArrayElementAtIndex(i);
            if (clipProperty == null)
            {
                continue;
            }

            SerializedProperty clipIdProperty = clipProperty.FindPropertyRelative("clipId");
            string title = string.IsNullOrWhiteSpace(clipIdProperty.stringValue)
                ? $"Clip {i}"
                : clipIdProperty.stringValue;

            clipProperty.isExpanded = EditorGUILayout.Foldout(clipProperty.isExpanded, title, true);
            if (!clipProperty.isExpanded)
            {
                continue;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(clipIdProperty);
                EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("channel"));
                EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("playMode"));
                EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("conflictPolicy"));
                EditorGUILayout.PropertyField(clipProperty.FindPropertyRelative("durationScale"));

                SerializedProperty tracksProperty = clipProperty.FindPropertyRelative("tracks");
                EditorGUILayout.PropertyField(tracksProperty, includeChildren: true);
                DrawAddTrackButtons(tracksProperty);

                if (GUILayout.Button("Remove Clip"))
                {
                    clipsProperty.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
        }
    }

    private void DrawAddClipButtons()
    {
        EditorGUILayout.LabelField("Add Common Clip", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            AddClipButton("Show", UIMotionClipIds.SHOW, UIMotionChannelIds.VISIBILITY, UIMotionConflictPolicy.StopAllChannels);
            AddClipButton("Hide", UIMotionClipIds.HIDE, UIMotionChannelIds.VISIBILITY, UIMotionConflictPolicy.StopAllChannels);
            AddClipButton("Hover", UIMotionClipIds.HOVER_IN, UIMotionChannelIds.INTERACTION, UIMotionConflictPolicy.StopSameChannel);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            AddClipButton("Hover Out", UIMotionClipIds.HOVER_OUT, UIMotionChannelIds.INTERACTION, UIMotionConflictPolicy.StopSameChannel);
            AddClipButton("Press", UIMotionClipIds.PRESS, UIMotionChannelIds.INTERACTION, UIMotionConflictPolicy.StopSameChannel);
            AddClipButton("Click", UIMotionClipIds.CLICK_PULSE, UIMotionChannelIds.FEEDBACK, UIMotionConflictPolicy.AllowParallel);
        }
    }

    private void AddClipButton(string label, string clipId, string channel, UIMotionConflictPolicy conflictPolicy)
    {
        if (!GUILayout.Button(label))
        {
            return;
        }

        int index = clipsProperty.arraySize;
        clipsProperty.InsertArrayElementAtIndex(index);
        SerializedProperty clipProperty = clipsProperty.GetArrayElementAtIndex(index);
        clipProperty.FindPropertyRelative("clipId").stringValue = clipId;
        clipProperty.FindPropertyRelative("channel").stringValue = channel;
        clipProperty.FindPropertyRelative("playMode").enumValueIndex = (int)UIMotionClipPlayMode.Parallel;
        clipProperty.FindPropertyRelative("conflictPolicy").enumValueIndex = (int)conflictPolicy;
        clipProperty.FindPropertyRelative("durationScale").floatValue = 1f;
        clipProperty.FindPropertyRelative("tracks").ClearArray();
    }

    private static void DrawAddTrackButtons(SerializedProperty tracksProperty)
    {
        EditorGUILayout.LabelField("Add Track", EditorStyles.miniBoldLabel);
        int columns = 3;
        for (int i = 0; i < TrackTypes.Length; i += columns)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int j = 0; j < columns && i + j < TrackTypes.Length; j++)
                {
                    (string label, Type trackType) = TrackTypes[i + j];
                    if (GUILayout.Button(label))
                    {
                        AddTrack(tracksProperty, trackType);
                    }
                }
            }
        }
    }

    private static void AddTrack(SerializedProperty tracksProperty, Type trackType)
    {
        object instance = Activator.CreateInstance(trackType);
        int index = tracksProperty.arraySize;
        tracksProperty.InsertArrayElementAtIndex(index);
        SerializedProperty element = tracksProperty.GetArrayElementAtIndex(index);
        element.managedReferenceValue = instance;
    }
}
