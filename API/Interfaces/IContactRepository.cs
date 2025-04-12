using API.Entities;

namespace API.Interfaces
{
    public interface IContactRepository
    {
        void AddContact(Contact contact);
        Task<bool> SaveAllAsync();
    }
}
