namespace WebApplication1.Services
{
    public interface IMessageCompletionService
    {
        Task<bool> MarkMessageAsDoneAsync(Guid jobId);
    }
}