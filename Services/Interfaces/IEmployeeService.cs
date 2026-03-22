using FunctionalitiesWebAPI.Models;

namespace FunctionalitiesWebAPI.Services.Interfaces;

public interface IEmployeeService
{
    Task<(IEnumerable<Employee> Data, int TotalCount)> GetEmployees(string search, int page, int pageSize);
    Task<Employee> GetByIdAsync(int id);
    Task AddAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task DeleteAsync(int id);
}
