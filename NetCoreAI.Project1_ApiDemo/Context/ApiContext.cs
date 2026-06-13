using Microsoft.EntityFrameworkCore;
using NetCoreAI.Project1_ApiDemo.Entities;

namespace NetCoreAI.Project1_ApiDemo.Context
{
    public class ApiContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server = DESKTOP-J4RS4FL; initial catalog = ApiAIDb; integrated security = true; trustservercertificate = true");
        }

        public DbSet<Customer> Customers { get; set; } 
    }
}
