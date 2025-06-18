namespace API.Interfaces
{
    public interface IUnitOfWork
    {
        IUserRepository UserRepository { get; }
        IContactRepository ContactRepository { get; }
        IUserChatHistoryRepository UserChatHistoryRepository { get; }
        IReminderRepository ReminderRepository { get; }
        ISurveyFormRepository SurveyFormRepository { get; }
        ISurveyFormDataRepository SurveyFormDataRepository { get; }
        Task<bool> Complete();
        bool HasChanges();
    }
}
