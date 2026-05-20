namespace Orange.UIFramework
{
    using DG.Tweening;
    using UnityEngine;

    public static class DOTweenRuntimeBootstrap
    {
        private const int TWEENERS_CAPACITY = 500;
        private const int SEQUENCES_CAPACITY = 200;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ConfigureCapacity()
        {
            // UI transitions can briefly create many nested sequences; pre-sizing avoids first-use hiccups.
            DOTween.Init().SetCapacity(TWEENERS_CAPACITY, SEQUENCES_CAPACITY);
        }
    }
}
