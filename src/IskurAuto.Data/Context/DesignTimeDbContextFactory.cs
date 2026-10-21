using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IskurAuto.Data.Context;

/// <summary>
/// Yalnızca `dotnet ef migrations ...` komutları tarafından kullanılır.
/// Neden gerekli: Aksi halde EF araçları API projesinin Program.cs'ini çalıştırır ve
/// başlangıçtaki veritabanı/seed kodu migration üretimi sırasında tetiklenir.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IskurAutoDbContext>
{
    public IskurAutoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IskurAutoDbContext>()
            .UseSqlite("Data Source=iskurauto.design.db")
            .Options;
        return new IskurAutoDbContext(options);
    }
}
