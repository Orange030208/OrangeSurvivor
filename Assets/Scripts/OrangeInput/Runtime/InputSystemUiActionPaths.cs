using System;
using UnityEngine;

namespace Orange.Input
{
    [Serializable]
    public struct InputSystemUiActionPaths
    {
        [SerializeField] private string pointActionPath;
        [SerializeField] private string clickActionPath;
        [SerializeField] private string scrollActionPath;
        [SerializeField] private string navigationActionPath;
        [SerializeField] private string submitActionPath;
        [SerializeField] private string cancelActionPath;

        public InputSystemUiActionPaths(
            string pointActionPath,
            string clickActionPath,
            string scrollActionPath,
            string navigationActionPath,
            string submitActionPath,
            string cancelActionPath)
        {
            this.pointActionPath = pointActionPath;
            this.clickActionPath = clickActionPath;
            this.scrollActionPath = scrollActionPath;
            this.navigationActionPath = navigationActionPath;
            this.submitActionPath = submitActionPath;
            this.cancelActionPath = cancelActionPath;
        }

        public static InputSystemUiActionPaths Default => new(
            "UI/Point",
            "UI/Click",
            "UI/Scroll",
            "UI/Navigate",
            "UI/Submit",
            "UI/Cancel");

        public string PointActionPath => string.IsNullOrWhiteSpace(pointActionPath) ? Default.pointActionPath : pointActionPath;
        public string ClickActionPath => string.IsNullOrWhiteSpace(clickActionPath) ? Default.clickActionPath : clickActionPath;
        public string ScrollActionPath => string.IsNullOrWhiteSpace(scrollActionPath) ? Default.scrollActionPath : scrollActionPath;
        public string NavigationActionPath => string.IsNullOrWhiteSpace(navigationActionPath) ? Default.navigationActionPath : navigationActionPath;
        public string SubmitActionPath => string.IsNullOrWhiteSpace(submitActionPath) ? Default.submitActionPath : submitActionPath;
        public string CancelActionPath => string.IsNullOrWhiteSpace(cancelActionPath) ? Default.cancelActionPath : cancelActionPath;
    }
}
