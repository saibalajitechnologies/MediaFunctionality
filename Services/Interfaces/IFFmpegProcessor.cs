namespace FunctionalitiesWebAPI.Services.Interfaces
{
    public interface IFFmpegProcessor
    {
        Task<string> RunCommand(string args);
        Task<string> RunCommandWithOutput(string args); // <--- add this
    }
}
