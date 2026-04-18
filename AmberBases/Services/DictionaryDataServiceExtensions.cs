using AmberBases.Core.Models.Dictionaries;
using System.Collections.Generic;

namespace AmberBases.Services;

public static class DictionaryDataServiceExtensions
{
    public static List<SystemProvider> GetSystemProviders(this IDictionaryDataService service, string dbPath) =>
        service.GetItems<SystemProvider>(dbPath);
    
    public static void AddSystemProvider(this IDictionaryDataService service, SystemProvider item, string dbPath) =>
        service.AddItem(item, dbPath);
    
    public static void UpdateSystemProvider(this IDictionaryDataService service, SystemProvider item, string dbPath) =>
        service.UpdateItem(item, dbPath);
    
    public static void DeleteSystemProvider(this IDictionaryDataService service, int id, string dbPath) =>
        service.DeleteItem<SystemProvider>(id, dbPath);

    public static List<ProfileSystem> GetProfileSystems(this IDictionaryDataService service, string dbPath) =>
        service.GetItems<ProfileSystem>(dbPath);
    
    public static void AddProfileSystem(this IDictionaryDataService service, ProfileSystem item, string dbPath) =>
        service.AddItem(item, dbPath);
    
    public static void UpdateProfileSystem(this IDictionaryDataService service, ProfileSystem item, string dbPath) =>
        service.UpdateItem(item, dbPath);
    
    public static void DeleteProfileSystem(this IDictionaryDataService service, int id, string dbPath) =>
        service.DeleteItem<ProfileSystem>(id, dbPath);

    public static List<Color> GetColors(this IDictionaryDataService service, string dbPath) =>
        service.GetItems<Color>(dbPath);
    
    public static void AddColor(this IDictionaryDataService service, Color item, string dbPath) =>
        service.AddItem(item, dbPath);
    
    public static void UpdateColor(this IDictionaryDataService service, Color item, string dbPath) =>
        service.UpdateItem(item, dbPath);
    
    public static void DeleteColor(this IDictionaryDataService service, int id, string dbPath) =>
        service.DeleteItem<Color>(id, dbPath);

    public static List<StandartBarLength> GetStandartBarLengths(this IDictionaryDataService service, string dbPath) =>
        service.GetItems<StandartBarLength>(dbPath);
    
    public static void AddStandartBarLength(this IDictionaryDataService service, StandartBarLength item, string dbPath) =>
        service.AddItem(item, dbPath);
    
    public static void UpdateStandartBarLength(this IDictionaryDataService service, StandartBarLength item, string dbPath) =>
        service.UpdateItem(item, dbPath);
    
    public static void DeleteStandartBarLength(this IDictionaryDataService service, int id, string dbPath) =>
        service.DeleteItem<StandartBarLength>(id, dbPath);

    public static List<ProfileArticle> GetProfileArticles(this IDictionaryDataService service, string dbPath) =>
        service.GetItems<ProfileArticle>(dbPath);
    
    public static void AddProfileArticle(this IDictionaryDataService service, ProfileArticle item, string dbPath) =>
        service.AddItem(item, dbPath);
    
    public static void UpdateProfileArticle(this IDictionaryDataService service, ProfileArticle item, string dbPath) =>
        service.UpdateItem(item, dbPath);
    
    public static void DeleteProfileArticle(this IDictionaryDataService service, int id, string dbPath) =>
        service.DeleteItem<ProfileArticle>(id, dbPath);

    public static List<ProfileType> GetProfileTypes(this IDictionaryDataService service, string dbPath) =>
        service.GetItems<ProfileType>(dbPath);
    
    public static void AddProfileType(this IDictionaryDataService service, ProfileType item, string dbPath) =>
        service.AddItem(item, dbPath);
    
    public static void UpdateProfileType(this IDictionaryDataService service, ProfileType item, string dbPath) =>
        service.UpdateItem(item, dbPath);
    
    public static void DeleteProfileType(this IDictionaryDataService service, int id, string dbPath) =>
        service.DeleteItem<ProfileType>(id, dbPath);

    public static List<CProfile> GetCProfiles(this IDictionaryDataService service, string dbPath) =>
        service.GetItems<CProfile>(dbPath);
    
    public static void AddCProfile(this IDictionaryDataService service, CProfile item, string dbPath) =>
        service.AddItem(item, dbPath);
    
    public static void UpdateCProfile(this IDictionaryDataService service, CProfile item, string dbPath) =>
        service.UpdateItem(item, dbPath);
    
    public static void DeleteCProfile(this IDictionaryDataService service, int id, string dbPath) =>
        service.DeleteItem<CProfile>(id, dbPath);

    public static List<Applicability> GetApplicabilities(this IDictionaryDataService service, string dbPath) =>
        service.GetItems<Applicability>(dbPath);
    
    public static void AddApplicability(this IDictionaryDataService service, Applicability item, string dbPath) =>
        service.AddItem(item, dbPath);
    
    public static void UpdateApplicability(this IDictionaryDataService service, Applicability item, string dbPath) =>
        service.UpdateItem(item, dbPath);
    
    public static void DeleteApplicability(this IDictionaryDataService service, int id, string dbPath) =>
        service.DeleteItem<Applicability>(id, dbPath);

    public static List<Customer> GetCustomers(this IDictionaryDataService service, string dbPath) =>
        service.GetItems<Customer>(dbPath);
    
    public static void AddCustomer(this IDictionaryDataService service, Customer item, string dbPath) =>
        service.AddItem(item, dbPath);
    
    public static void UpdateCustomer(this IDictionaryDataService service, Customer item, string dbPath) =>
        service.UpdateItem(item, dbPath);
    
    public static void DeleteCustomer(this IDictionaryDataService service, int id, string dbPath) =>
        service.DeleteItem<Customer>(id, dbPath);

    public static List<CustomerContact> GetCustomerContacts(this IDictionaryDataService service, string dbPath) =>
        service.GetItems<CustomerContact>(dbPath);
    
    public static void AddCustomerContact(this IDictionaryDataService service, CustomerContact item, string dbPath) =>
        service.AddItem(item, dbPath);
    
    public static void UpdateCustomerContact(this IDictionaryDataService service, CustomerContact item, string dbPath) =>
        service.UpdateItem(item, dbPath);
    
    public static void DeleteCustomerContact(this IDictionaryDataService service, int id, string dbPath) =>
        service.DeleteItem<CustomerContact>(id, dbPath);

    public static List<CoatingType> GetCoatingTypes(this IDictionaryDataService service, string dbPath) =>
        service.GetItems<CoatingType>(dbPath);
    
    public static void AddCoatingType(this IDictionaryDataService service, CoatingType item, string dbPath) =>
        service.AddItem(item, dbPath);
    
    public static void UpdateCoatingType(this IDictionaryDataService service, CoatingType item, string dbPath) =>
        service.UpdateItem(item, dbPath);
    
    public static void DeleteCoatingType(this IDictionaryDataService service, int id, string dbPath) =>
        service.DeleteItem<CoatingType>(id, dbPath);
}
