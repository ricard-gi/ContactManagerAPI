using Microsoft.EntityFrameworkCore;

namespace contacts.Models
{

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Contact> Contacts { get; set; }
    }


}