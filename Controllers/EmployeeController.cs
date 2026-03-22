using FunctionalitiesWebAPI.Models;
using FunctionalitiesWebAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FunctionalitiesWebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _service;

    public EmployeeController(IEmployeeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get(string search, int page = 1, int pageSize = 10)
    {
        var (data, total) = await _service.GetEmployees(search, page, pageSize);

        return Ok(new
        {
            Total = total,
            Data = data
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(Employee emp)
    {
        await _service.AddAsync(emp);
        return Ok(emp);
    }

    [HttpPut]
    public async Task<IActionResult> Update(Employee emp)
    {
        await _service.UpdateAsync(emp);
        return Ok(emp);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok();
    }
}
