using API.DTOs;
using API.Entities;
using API.Helpers;

namespace API.Interfaces
{
    public interface IContactRepository
    {
        void AddContact(Contact contact);
        Task<bool> SaveAllAsync();
    }
}
