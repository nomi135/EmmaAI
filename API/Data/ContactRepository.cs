using API.Entities;
using API.Interfaces;

namespace API.Data
{
    public class ContactRepository(DataContext context) : IContactRepository
    {
        public void AddContact(Contact contact)
        {
            context.Contacts.Add(contact);
        }
        
    }
}
