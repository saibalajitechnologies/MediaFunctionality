using FunctionalitiesWebAPI.DTO;
using FunctionalitiesWebAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FunctionalitiesWebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestController : ControllerBase
{
    private readonly ITestUserService _service;

    public TestController(ITestUserService service)
    {
        _service = service;
    }

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _service.GetAll();
        return Ok(users);
    }

    [HttpGet("GetById/{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _service.GetById(id);

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpPost("CreateUserData")]
    public async Task<IActionResult> CreateUserData([FromBody] TestUpdateDto model)
    {
        var id = await _service.CreateUser(model);

        return Ok(new { Id = id });
    }

    [HttpDelete("DeleteUserData/{id}")]
    public async Task<IActionResult> DeleteUserData(int id)
    {
        var result = await _service.DeleteUser(id);

        if (!result)
            return NotFound();

        return Ok("User Deleted");
    }

    [HttpPost("BulkUpdate")]
    public async Task<IActionResult> BulkUpdate([FromBody] List<TestUpdateDto> users)
    {
        if (users == null || users.Count == 0)
            return BadRequest("No Users Provided");

        await _service.UpdateUsers(users);

        return Ok("Users Updated Successfully");
    }
}
