using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Orange.Input
{
    public static class InputSystemUiBinder
    {
        public static bool Configure(
            EventSystem eventSystem,
            IInputActionProvider inputProvider,
            InputSystemUiActionPaths actionPaths)
        {
            if (eventSystem == null)
            {
                Debug.LogError($"{nameof(InputSystemUiBinder)} requires an explicit {nameof(EventSystem)} reference.");
                return false;
            }

            if (inputProvider == null)
            {
                Debug.LogError($"{nameof(InputSystemUiBinder)} requires an explicit {nameof(IInputActionProvider)} reference.", eventSystem);
                return false;
            }

            InputActionAsset asset = inputProvider.ActionsAsset;
            if (asset == null)
            {
                Debug.LogError($"{nameof(InputSystemUiBinder)} cannot configure '{eventSystem.name}' because no {nameof(InputActionAsset)} is assigned.", eventSystem);
                return false;
            }

            if (!TryCreateReference(inputProvider, actionPaths.PointActionPath, eventSystem, out InputActionReference point) ||
                !TryCreateReference(inputProvider, actionPaths.ClickActionPath, eventSystem, out InputActionReference click) ||
                !TryCreateReference(inputProvider, actionPaths.ScrollActionPath, eventSystem, out InputActionReference scroll) ||
                !TryCreateReference(inputProvider, actionPaths.NavigationActionPath, eventSystem, out InputActionReference navigation) ||
                !TryCreateReference(inputProvider, actionPaths.SubmitActionPath, eventSystem, out InputActionReference submit) ||
                !TryCreateReference(inputProvider, actionPaths.CancelActionPath, eventSystem, out InputActionReference cancel))
            {
                return false;
            }

            StandaloneInputModule standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneModule != null)
            {
                standaloneModule.enabled = false;
            }

            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            inputModule.actionsAsset = asset;
            inputModule.point = point;
            inputModule.leftClick = click;
            inputModule.scrollWheel = scroll;
            inputModule.move = navigation;
            inputModule.submit = submit;
            inputModule.cancel = cancel;
            inputModule.moveRepeatDelay = 0.45f;
            inputModule.moveRepeatRate = 0.08f;
            inputModule.enabled = true;
            return true;
        }

        private static bool TryCreateReference(
            IInputActionProvider inputProvider,
            string actionPath,
            Object logContext,
            out InputActionReference reference)
        {
            reference = null;
            if (!inputProvider.TryFindAction(actionPath, out InputAction action))
            {
                Debug.LogError($"{nameof(InputSystemUiBinder)} could not find UI action '{actionPath}'.", logContext);
                return false;
            }

            reference = InputActionReference.Create(action);
            return true;
        }
    }
}
