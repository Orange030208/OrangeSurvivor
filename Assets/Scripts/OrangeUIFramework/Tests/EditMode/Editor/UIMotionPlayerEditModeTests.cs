using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Orange.UIFramework.Tests
{
    public sealed class UIMotionPlayerEditModeTests
    {
        [UnityTest]
        public IEnumerator RefreshDefaultsOnEnable_ReenabledViewUsesCurrentMotionOrigin()
        {
            GameObject viewObject = new GameObject("UIMotionRefreshDefaultsTest", typeof(RectTransform));
            UIMotionDefinition definition = CreateMoveDefinition();

            try
            {
                RectTransform rectTransform = viewObject.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(10f, 10f);
                viewObject.AddComponent<CanvasGroup>();
                UIMotionPlayer player = viewObject.AddComponent<UIMotionPlayer>();
                TestReflection.SetField(player, "definition", definition);

                yield return null;

                viewObject.SetActive(false);
                rectTransform.anchoredPosition = new Vector2(120f, 80f);
                viewObject.SetActive(true);

                yield return null;

                player.SetHiddenImmediate();

                Assert.That(rectTransform.anchoredPosition.x, Is.EqualTo(120f).Within(0.01f));
                Assert.That(rectTransform.anchoredPosition.y, Is.EqualTo(60f).Within(0.01f));
            }
            finally
            {
                DOTween.KillAll();
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(definition);
            }
        }

        private static UIMotionDefinition CreateMoveDefinition()
        {
            UIMotionDefinition definition = ScriptableObject.CreateInstance<UIMotionDefinition>();
            UIMotionClipDefinition hideClip = new UIMotionClipDefinition();
            UIMoveMotionTrack moveTrack = new UIMoveMotionTrack();

            TestReflection.SetField(moveTrack, "fromMode", UIMotionVector2ValueMode.Initial);
            TestReflection.SetField(moveTrack, "toMode", UIMotionVector2ValueMode.InitialPlusOffset);
            TestReflection.SetField(moveTrack, "toValue", new Vector2(0f, -20f));

            TestReflection.SetField(hideClip, "clipId", UIMotionClipIds.HIDE);
            TestReflection.SetField(hideClip, "tracks", new List<UIMotionTrackDefinition> { moveTrack });
            TestReflection.SetField(definition, "clips", new List<UIMotionClipDefinition> { hideClip });
            return definition;
        }
    }
}
