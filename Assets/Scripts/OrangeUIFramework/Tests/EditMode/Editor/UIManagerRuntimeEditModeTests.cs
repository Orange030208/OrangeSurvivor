using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Orange.UIFramework.Tests
{
    public sealed class UIManagerRuntimeEditModeTests
    {
        [UnityTest]
        public IEnumerator OpenAndClosePage_UpdatesStackAndReusesPooledInstance()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using UIManagerRuntimeTestHarness harness = new UIManagerRuntimeTestHarness();
                UIManager manager = harness.Manager;

                ViewHandle<RuntimeTestPageView> firstHandle = await manager.OpenPageAsync<RuntimeTestPageView>();
                RuntimeTestPageView firstView = firstHandle.View;

                UIRuntimeDiagnostics opened = manager.GetRuntimeDiagnostics();
                Assert.That(opened.PageStack.Count, Is.EqualTo(1));
                Assert.That(opened.PageStack[0].ViewTypeName, Is.EqualTo(nameof(RuntimeTestPageView)));
                Assert.That(opened.PageStack[0].InputActive, Is.True);
                Assert.That(firstView.IsOpen, Is.True);

                await firstHandle.CloseAsync();

                UIRuntimeDiagnostics closed = manager.GetRuntimeDiagnostics();
                Assert.That(closed.PageStack.Count, Is.EqualTo(0));
                Assert.That(FindPoolCount(closed, nameof(RuntimeTestPageView)), Is.EqualTo(1));

                ViewHandle<RuntimeTestPageView> secondHandle = await manager.OpenPageAsync<RuntimeTestPageView>();
                Assert.That(secondHandle.View, Is.SameAs(firstView));

                await secondHandle.CloseAsync();
            });
        }

        [UnityTest]
        public IEnumerator LegacyUIPageBase_CanOpenAndCloseThroughOrangeManagerTypeApi()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using UIManagerRuntimeTestHarness harness = new UIManagerRuntimeTestHarness();
                UIManager manager = harness.Manager;

                ViewHandle handle = await manager.OpenPageAsync(typeof(LegacyRuntimeTestPageView), "legacyPayload");

                Assert.That(handle.IsValid, Is.True);
                Assert.That(handle.View, Is.InstanceOf<LegacyRuntimeTestPageView>());
                LegacyRuntimeTestPageView page = (LegacyRuntimeTestPageView)handle.View;
                Assert.That(page.OpenedCount, Is.EqualTo(1));
                Assert.That(page.LastOpenContext.GetPayload<string>(), Is.EqualTo("legacyPayload"));
                Assert.That(manager.IsOpen(typeof(LegacyRuntimeTestPageView)), Is.True);

                await manager.ClosePageAsync(typeof(LegacyRuntimeTestPageView));

                Assert.That(page.ClosedCount, Is.EqualTo(1));
                Assert.That(manager.IsOpen(typeof(LegacyRuntimeTestPageView)), Is.False);
            });
        }

        [UnityTest]
        public IEnumerator LatestReplaceRequest_WinsWhenPreviousOpenIsStillPending()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using UIManagerRuntimeTestHarness harness = new UIManagerRuntimeTestHarness();
                UIManager manager = harness.Manager;

                UniTask<ViewHandle<RuntimeSlowOpeningPageView>> staleTask = manager.ReplacePageAsync<RuntimeSlowOpeningPageView>();
                await UniTask.Yield(PlayerLoopTiming.Update);
                UniTask<ViewHandle<SecondRuntimeTestPageView>> latestTask = manager.ReplacePageAsync<SecondRuntimeTestPageView>();

                bool staleCancelled = false;
                try
                {
                    await staleTask;
                }
                catch (OperationCanceledException)
                {
                    staleCancelled = true;
                }

                ViewHandle<SecondRuntimeTestPageView> latestHandle = await latestTask;

                UIRuntimeDiagnostics diagnostics = manager.GetRuntimeDiagnostics();
                Assert.That(staleCancelled, Is.True);
                Assert.That(diagnostics.PageStack.Count, Is.EqualTo(1));
                Assert.That(diagnostics.PageStack[0].ViewTypeName, Is.EqualTo(nameof(SecondRuntimeTestPageView)));

                await latestHandle.CloseAsync();
            });
        }

        [UnityTest]
        public IEnumerator CloseHandleTwice_InvokesClosingLifecycleOnce()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using UIManagerRuntimeTestHarness harness = new UIManagerRuntimeTestHarness();
                UIManager manager = harness.Manager;

                ViewHandle<RuntimeSlowClosingPageView> handle = await manager.OpenPageAsync<RuntimeSlowClosingPageView>();

                UniTask firstClose = handle.CloseAsync(CloseReason.Normal);
                await UniTask.Yield(PlayerLoopTiming.Update);
                UniTask secondClose = handle.CloseAsync(CloseReason.Back);

                await UniTask.WhenAll(firstClose, secondClose);

                Assert.That(handle.View.ClosingStartedCount, Is.EqualTo(1));
                Assert.That(handle.View.ClosedCount, Is.EqualTo(1));
                Assert.That(handle.View.LastCloseReason, Is.EqualTo(CloseReason.Normal));
                Assert.That(manager.GetRuntimeDiagnostics().PageStack.Count, Is.EqualTo(0));
            });
        }

        [UnityTest]
        public IEnumerator PopupOutsideClick_ClosesTopPopup()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using UIManagerRuntimeTestHarness harness = new UIManagerRuntimeTestHarness();
                UIManager manager = harness.Manager;

                PopupOptions options = new PopupOptions(
                    screenPosition: new Vector2(32f, 32f),
                    closeOnOutsideClick: true,
                    useScreenPosition: true);

                ViewHandle<RuntimeTestPopupView> popup = await manager.ShowPopupAsync<RuntimeTestPopupView>(options: options);
                UIRuntimeDiagnostics opened = manager.GetRuntimeDiagnostics();
                Assert.That(opened.PopupStack.Count, Is.EqualTo(1));
                Assert.That(opened.PopupOutsideClickBlocker.Active, Is.True);
                Assert.That(opened.PopupOutsideClickBlocker.ClickCanCloseTopView, Is.True);

                Button outsideClickButton = harness.FindBlockerButton("PopupLayer", "PopupOutsideClickBlocker");
                outsideClickButton.onClick.Invoke();

                await UniTask.WaitUntil(() => manager.GetRuntimeDiagnostics().PopupStack.Count == 0);

                UIRuntimeDiagnostics closed = manager.GetRuntimeDiagnostics();
                Assert.That(closed.PopupStack.Count, Is.EqualTo(0));
                Assert.That(popup.View.IsOpen, Is.False);
            });
        }

        [UnityTest]
        public IEnumerator ModalResult_CompletesOnlyOnceAndBlocksUnderlyingInput()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using UIManagerRuntimeTestHarness harness = new UIManagerRuntimeTestHarness();
                UIManager manager = harness.Manager;

                ViewHandle<RuntimeTestPageView> page = await manager.OpenPageAsync<RuntimeTestPageView>();
                UniTask<ModalResult<bool>> modalTask = manager.ShowModalAsync<RuntimeTestModalView, bool>();
                await UniTask.WaitUntil(() => RuntimeTestModalView.LastInstance != null && RuntimeTestModalView.LastInstance.IsOpen);

                UIRuntimeDiagnostics modalOpen = manager.GetRuntimeDiagnostics();
                Assert.That(modalOpen.ModalStack.Count, Is.EqualTo(1));
                Assert.That(modalOpen.ModalMask.Active, Is.True);
                Assert.That(modalOpen.Input.ModalBlocksUnderlyingInput, Is.True);
                Assert.That(page.View.InputActive, Is.False);
                Assert.That(RuntimeTestModalView.LastInstance.InputActive, Is.True);

                RuntimeTestModalView.LastInstance.ConfirmForTest(true);
                RuntimeTestModalView.LastInstance.CancelForTest();

                ModalResult<bool> result = await modalTask;

                Assert.That(result.Confirmed, Is.True);
                Assert.That(result.Value, Is.True);
                Assert.That(result.CloseReason, Is.EqualTo(CloseReason.Completed));
                Assert.That(manager.GetRuntimeDiagnostics().ModalStack.Count, Is.EqualTo(0));

                await page.CloseAsync();
            });
        }

        [UnityTest]
        public IEnumerator ModalMaskClick_CompletesFallbackCancelResult()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using UIManagerRuntimeTestHarness harness = new UIManagerRuntimeTestHarness();
                UIManager manager = harness.Manager;

                UniTask<ModalResult<bool>> modalTask = manager.ShowModalAsync<RuntimeTestModalView, bool>();
                await UniTask.WaitUntil(() => RuntimeTestModalView.LastInstance != null && RuntimeTestModalView.LastInstance.IsOpen);

                Button maskButton = harness.FindBlockerButton("ModalMaskLayer", "ModalMask");
                maskButton.onClick.Invoke();

                ModalResult<bool> result = await modalTask;

                Assert.That(result.Confirmed, Is.False);
                Assert.That(result.CloseReason, Is.EqualTo(CloseReason.OutsideClick));
                Assert.That(manager.GetRuntimeDiagnostics().ModalMask.Active, Is.False);
            });
        }

        [UnityTest]
        public IEnumerator TooltipNearScreenEdge_StoresClampedPlacementDiagnostics()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using UIManagerRuntimeTestHarness harness = new UIManagerRuntimeTestHarness();
                UIManager manager = harness.Manager;

                TooltipOptions options = new TooltipOptions(
                    screenPosition: new Vector2(1919f, 1079f),
                    followPointer: true,
                    margin: 16f,
                    preferredAnchor: FloatingViewAnchor.BottomRight,
                    useScreenPosition: true);

                await manager.ShowTooltipAsync<RuntimeTestTooltipView>(new object(), options);

                ViewDiagnostics tooltip = FindOpenView(manager.GetRuntimeDiagnostics(), nameof(RuntimeTestTooltipView));
                Assert.That(tooltip.HasPlacement, Is.True);
                Assert.That(tooltip.PlacementWasFlipped || tooltip.PlacementWasClamped, Is.True);
                Assert.That(tooltip.LocalRect.xMin, Is.GreaterThanOrEqualTo(tooltip.BoundsRect.xMin - 0.5f));
                Assert.That(tooltip.LocalRect.xMax, Is.LessThanOrEqualTo(tooltip.BoundsRect.xMax + 0.5f));
                Assert.That(tooltip.LocalRect.yMin, Is.GreaterThanOrEqualTo(tooltip.BoundsRect.yMin - 0.5f));
                Assert.That(tooltip.LocalRect.yMax, Is.LessThanOrEqualTo(tooltip.BoundsRect.yMax + 0.5f));

                manager.HideTooltip();
                await UniTask.WaitUntil(() => !manager.GetRuntimeDiagnostics().Tooltip.HasTooltip);
            });
        }

        [UnityTest]
        public IEnumerator RuntimeDiagnostics_CapturesStacksInputMaskAndTooltip()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using UIManagerRuntimeTestHarness harness = new UIManagerRuntimeTestHarness();
                UIManager manager = harness.Manager;

                await manager.OpenPageAsync<RuntimeTestPageView>();
                await manager.ShowPopupAsync<RuntimeTestPopupView>(
                    options: new PopupOptions(screenPosition: new Vector2(48f, 48f), closeOnOutsideClick: true, useScreenPosition: true));
                UniTask<ModalResult<bool>> modalTask = manager.ShowModalAsync<RuntimeTestModalView, bool>();
                await UniTask.WaitUntil(() => RuntimeTestModalView.LastInstance != null && RuntimeTestModalView.LastInstance.IsOpen);
                await manager.ShowTooltipAsync<RuntimeTestTooltipView>(
                    new object(),
                    new TooltipOptions(screenPosition: new Vector2(64f, 64f), followPointer: true, useScreenPosition: true));

                UIRuntimeDiagnostics diagnostics = manager.GetRuntimeDiagnostics();

                Assert.That(diagnostics.PageStack.Count, Is.EqualTo(1));
                Assert.That(diagnostics.PopupStack.Count, Is.EqualTo(1));
                Assert.That(diagnostics.ModalStack.Count, Is.EqualTo(1));
                Assert.That(diagnostics.Tooltip.HasTooltip, Is.True);
                Assert.That(diagnostics.ModalMask.Active, Is.True);
                Assert.That(diagnostics.PopupOutsideClickBlocker.Exists, Is.True);
                Assert.That(diagnostics.Input.ModalBlocksUnderlyingInput, Is.True);
                Assert.That(diagnostics.Input.InputActiveViewCount, Is.EqualTo(1));
                Assert.That(diagnostics.Input.TooltipBlocksRaycasts, Is.False);

                RuntimeTestModalView.LastInstance.CancelForTest();
                await modalTask;
            });
        }

        private static int FindPoolCount(UIRuntimeDiagnostics diagnostics, string viewTypeName)
        {
            for (int i = 0; i < diagnostics.Pools.Count; i++)
            {
                PoolDiagnostics pool = diagnostics.Pools[i];
                if (pool.ViewTypeName == viewTypeName)
                {
                    return pool.CachedCount;
                }
            }

            return 0;
        }

        private static ViewDiagnostics FindOpenView(UIRuntimeDiagnostics diagnostics, string viewTypeName)
        {
            for (int i = 0; i < diagnostics.OpenViews.Count; i++)
            {
                ViewDiagnostics view = diagnostics.OpenViews[i];
                if (view.ViewTypeName == viewTypeName)
                {
                    return view;
                }
            }

            Assert.Fail($"Could not find open view '{viewTypeName}'.");
            return default;
        }
    }
}
