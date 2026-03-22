using FunctionalitiesWebAPI.DTO;
using FunctionalitiesWebAPI.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using Dapper;

namespace FunctionalitiesWebAPI.Services;

public class TestUserService : ITestUserService
{
    private readonly IConfiguration _configuration;

    public TestUserService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private IDbConnection Connection =>
        new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

    // Get All Users
    public async Task<IEnumerable<TestUpdateDto>> GetAll()
    {
        using var connection = Connection;

        var users = await connection.QueryAsync<TestUpdateDto>(
            "GetAllUsers",
            commandType: CommandType.StoredProcedure);

        return users;
    }

    // Get By Id
    public async Task<TestUpdateDto?> GetById(int id)
    {
        using var connection = Connection;

        var user = await connection.QueryFirstOrDefaultAsync<TestUpdateDto>(
            "GetUserById",
            new { Id = id },
            commandType: CommandType.StoredProcedure);

        return user;
    }

    // Create
    public async Task<int> CreateUser(TestUpdateDto user)
    {
        using var connection = Connection;

        var id = await connection.ExecuteScalarAsync<int>(
            "CreateUser",
            new
            {
                user.Name,
                user.isSelected
            },
            commandType: CommandType.StoredProcedure);

        return id;
    }

    // Delete
    public async Task<bool> DeleteUser(int id)
    {
        using var connection = Connection;

        var rows = await connection.ExecuteAsync(
            "DeleteUser",
            new { Id = id },
            commandType: CommandType.StoredProcedure);

        return rows > 0;
    }

    // Bulk Update
    public async Task<bool> UpdateUsers(List<TestUpdateDto> users)
    {
        var table = ConvertToDataTable(users);

        using var connection = Connection;

        var parameters = new DynamicParameters();

        parameters.Add("@Users",
            table.AsTableValuedParameter("testUserTableType"));

        await connection.ExecuteAsync(
            "UpdateUsers",
            parameters,
            commandType: CommandType.StoredProcedure);

        return true;
    }

    private DataTable ConvertToDataTable(List<TestUpdateDto> users)
    {
        var table = new DataTable();

        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("isSelected", typeof(bool));

        foreach (var user in users)
        {
            table.Rows.Add(user.Id, user.Name, user.isSelected);
        }

        return table;
    }

}
