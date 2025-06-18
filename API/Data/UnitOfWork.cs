using API.Interfaces;

namespace API.Data
{
    public class UnitOfWork(DataContext context, IUserRepository userRepository, IContactRepository contactRepository, IUserChatHistoryRepository userChatHistoryRepository, 
                           IReminderRepository reminderRepository, ISurveyFormRepository surveyFormRepository, ISurveyFormDataRepository surveyFormDataRepository) : IUnitOfWork
    {
        public IUserRepository UserRepository => userRepository;

        public IContactRepository ContactRepository => contactRepository;

        public IUserChatHistoryRepository UserChatHistoryRepository => userChatHistoryRepository;

        public IReminderRepository ReminderRepository => reminderRepository;

        public ISurveyFormRepository SurveyFormRepository => surveyFormRepository;

        public ISurveyFormDataRepository SurveyFormDataRepository => surveyFormDataRepository;

        public async Task<bool> Complete()
        {
            return await context.SaveChangesAsync() > 0;
        }

        public bool HasChanges()
        {
            return context.ChangeTracker.HasChanges();
        }
    }
}
