using FunctionalitiesWebAPI.Data;
using FunctionalitiesWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FunctionalitiesWebAPI.Services;

public class EmployeeService
{
    private readonly ApplicationDbContext _context;

    public EmployeeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Employee> Data, int TotalCount)> GetEmployees(string search, int page, int pageSize)
    {
        var query = _context.Employees.AsQueryable();

        // 🔍 Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Name!.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return (data, totalCount);
    }

    public async Task<Employee> GetByIdAsync(int id)
    {
         var emp = await _context.Employees.FindAsync(id);
        if (emp == null)
            throw new Exception("Employee not found");
        return emp;
    }

    public async Task AddAsync(Employee employee)
    {
        if (employee == null)
            throw new ArgumentNullException(nameof(employee));

        await _context.Employees.AddAsync(employee);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Employee employee)
    {
        var existing = await _context.Employees.FindAsync(employee.Id);

        if (existing == null)
            throw new Exception("Employee not found");

        existing.Name = employee.Name;
        existing.Salary = employee.Salary;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var emp = await _context.Employees.FindAsync(id);

        if (emp == null)
            throw new Exception("Employee not found");

        _context.Employees.Remove(emp);
        await _context.SaveChangesAsync();
    }

}
