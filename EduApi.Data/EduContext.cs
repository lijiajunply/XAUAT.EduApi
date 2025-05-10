using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EduApi.Data;

public class EduContext(DbContextOptions<EduContext> options) : DbContext
{
    
}

[Serializable]
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EduContext>
{
    public EduContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EduContext>();
        optionsBuilder.UseSqlite("Data Source=Data.db");
        return new EduContext(optionsBuilder.Options);
    }
}