public interface IAnimatable
{
    // 底层Animator操作
    void SetBool(int id, bool value);
    void SetTrigger(int id);
    void SetFloat(int id, float value);
    void SetInteger(int id, int value);
        
    // 直接用字符串操作
    void SetBool(string paramName, bool value);
    void SetTrigger(string paramName);
    void SetFloat(string paramName, float value);
    void SetInteger(string paramName, int value);

    // 播放动画状态 参数用字符串或哈希ID
    void PlayState(string stateName);
    void PlayState(int stateHash);
    void SetPlaybackSpeed(float speed);
    void ResetPlaybackSpeed();
    bool IsCurrentState(int stateHash, int layerIndex = 0);
    float GetCurrentStateNormalizedTime(int layerIndex = 0);
}
