using System;
using System.Collections.Generic;
using AmberBases.Core.Models.Dictionaries;

namespace AmberBases.Services
{
    public interface IDictionaryDataService
    {
        void InitializeDatabase(string dbPath);
        
        // SystemProvider
        List<SystemProvider> GetSystemProviders(string dbPath);
        void AddSystemProvider(SystemProvider provider, string dbPath);
        void UpdateSystemProvider(SystemProvider provider, string dbPath);
        void DeleteSystemProvider(int id, string dbPath);
        
        // ProfileSystem
        List<ProfileSystem> GetProfileSystems(string dbPath);
        void AddProfileSystem(ProfileSystem system, string dbPath);
        void UpdateProfileSystem(ProfileSystem system, string dbPath);
        void DeleteProfileSystem(int id, string dbPath);
        
        // Color
        List<Color> GetColors(string dbPath);
        void AddColor(Color color, string dbPath);
        void UpdateColor(Color color, string dbPath);
        void DeleteColor(int id, string dbPath);
        
        // WhipLength
        List<WhipLength> GetWhipLengths(string dbPath);
        void AddWhipLength(WhipLength length, string dbPath);
        void UpdateWhipLength(WhipLength length, string dbPath);
        void DeleteWhipLength(int id, string dbPath);
        
        // ProfileArticle
        List<ProfileArticle> GetProfileArticles(string dbPath);
        void AddProfileArticle(ProfileArticle article, string dbPath);
        void UpdateProfileArticle(ProfileArticle article, string dbPath);
        void DeleteProfileArticle(int id, string dbPath);
        
        // ProfileType
        List<ProfileType> GetProfileTypes(string dbPath);
        void AddProfileType(ProfileType item, string dbPath);
        void UpdateProfileType(ProfileType item, string dbPath);
        void DeleteProfileType(int id, string dbPath);

        // Applicability
        List<Applicability> GetApplicabilities(string dbPath);
        void AddApplicability(Applicability item, string dbPath);
        void UpdateApplicability(Applicability item, string dbPath);
        void DeleteApplicability(int id, string dbPath);
        
        // Customer
        List<Customer> GetCustomers(string dbPath);
        void AddCustomer(Customer customer, string dbPath);
        void UpdateCustomer(Customer customer, string dbPath);
        void DeleteCustomer(int id, string dbPath);
        
        // CustomerContact
        List<CustomerContact> GetCustomerContacts(string dbPath);
        void AddCustomerContact(CustomerContact contact, string dbPath);
        void UpdateCustomerContact(CustomerContact contact, string dbPath);
        void DeleteCustomerContact(int id, string dbPath);
        
        // CoatingType
        List<CoatingType> GetCoatingTypes(string dbPath);
        void AddCoatingType(CoatingType coatingType, string dbPath);
        void UpdateCoatingType(CoatingType coatingType, string dbPath);
        void DeleteCoatingType(int id, string dbPath);
    }
}
