using MagicTeste.Tabela;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


namespace MagicTeste.Context
{


    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        private readonly IConfiguration _config;

        public AppDbContext(IConfiguration config)
        {
            _config = config;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                _config.GetConnectionString("DatabaseConnection"));
        }


        public DbSet<TabelaWebHookJson> TabelaWebHookJson { get; set; }
    }

}
