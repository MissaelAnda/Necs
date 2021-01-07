namespace Necs
{
    /// <summary>
    /// System executed when registry lifecycle started
    /// </summary>
    public interface IStartSystem
    {
        void Start(Group group);
    }

    /// <summary>
    /// System executed only once when queued before process systems begin
    /// </summary>
    public interface IPreProcessSystem
    {
        void PreProcess(Group group);
    }

    /// <summary>
    /// System executed in process
    /// </summary>
    public interface IProcessSystem
    {
        void Process(Group group);
    }

    /// <summary>
    /// System executed only once when queued after process systems ends
    /// </summary>
    public interface IPostProcessSystem
    {
        void PostProcess(Group group);
    }

    /// <summary>
    /// System executed inmediatly after queued
    /// </summary>
    public interface ISingleFrameSystem
    {
        void SingleFrame(Group group);
    }

    /// <summary>
    /// System executed once on the end of registry's lifecycle
    /// </summary>
    public interface IEndSystem
    {
        void End(Group group);
    }
}
