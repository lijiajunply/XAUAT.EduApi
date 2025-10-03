using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using EduApi.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EduApi.Data;

public class EduContext(DbContextOptions<EduContext> options) : DbContext
{
    public DbSet<UserModel> Users { get; set; }
    public DbSet<ScoreResponse> Scores { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=Data.db");
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserModel>()
            .HasMany(u => u.ScoreResponses)
            .WithOne()
            .HasForeignKey(s => s.UserId)
            .HasPrincipalKey(u => u.Id);
    }
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

public static class DataTool
{
    public static string StringToHash(string s)
    {
        var data = Encoding.UTF8.GetBytes(s);
        var hash = MD5.HashData(data);
        var hashStringBuilder = new StringBuilder();
        foreach (var t in hash)
            hashStringBuilder.Append(t.ToString("x2"));
        return hashStringBuilder.ToString();
    }

    public static string ToHash(this object t) => StringToHash(t.ToString()!);

    public static string GetProperties<T>(T t)
    {
        StringBuilder builder = new StringBuilder();
        if (t == null) return builder.ToString();

        var properties = t.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

        if (properties.Length <= 0) return builder.ToString();

        foreach (var item in properties)
        {
            var name = item.Name;
            var value = item.GetValue(t, null);
            if (item.PropertyType.IsValueType || item.PropertyType.Name.StartsWith("String"))
            {
                builder.Append($"{name}:{value ?? "null"},");
            }
        }

        return builder.ToString();
    }
}

public abstract class DataModel
{
    public override string ToString() => $"{GetType()} : {DataTool.GetProperties(this)}";
    public string GetHashKey() => DataTool.StringToHash(ToString());
}