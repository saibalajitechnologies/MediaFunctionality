using FunctionalitiesWebAPI.DTO;

namespace FunctionalitiesWebAPI.Services.Interfaces;

public interface ITestUserService
{
    //Task UpdateUsers(List<TestUpdateDto> users);

    Task<IEnumerable<TestUpdateDto>> GetAll();

    Task<TestUpdateDto?> GetById(int id);

    Task<int> CreateUser(TestUpdateDto user);

    Task<bool> DeleteUser(int id);

    Task<bool> UpdateUsers(List<TestUpdateDto> users);
}
