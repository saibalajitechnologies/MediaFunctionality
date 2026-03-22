using FunctionalitiesWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FunctionalitiesWebAPI.Data;

public class ApplicationDbContext: DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)    {        
    }

    public DbSet<ApplicationUser> Users { get; set; }

    public DbSet<Employee> Employees { get; set; }
}
