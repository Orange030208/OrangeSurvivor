namespace Orange.Services
{
    /// <summary>
    /// 将一组服务注册安装到注册表中。
    /// </summary>
    public interface IServiceInstaller
    {
        void Install(IServiceRegistry registry);
    }
}
