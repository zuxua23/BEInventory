using Microsoft.EntityFrameworkCore;
using InventoryControl.Entity;
namespace InventoryControl.Database;

public class AppDBContext : DbContext
{
    public AppDBContext(DbContextOptions<AppDBContext> options)
            : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<User_Role> UserRoles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Role_Permission> RolePermissions { get; set; }
    public DbSet<Module> Modules { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Reader> Readers { get; set; }
    public DbSet<DO> DOs { get; set; }
    public DbSet<DODetail> DODetails { get; set; }
    public DbSet<DODetailTag> DODetailTags { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Transaction_Detail> TransactionDetails { get; set; }
    public DbSet<StockTaking> StockTakings { get; set; }
    public DbSet<StockTakingDetail> StockTakingDetails { get; set; }
    public DbSet<HistoryPrint> Histories { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //FOR ENUM CONVERSION
        modelBuilder.Entity<Tag>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        modelBuilder.Entity<HistoryPrint>()
            .Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(30);

        modelBuilder.Entity<Transaction>()
            .Property(x => x.TrsType)
            .HasConversion<string>()
            .HasMaxLength(30);

        modelBuilder.Entity<DO>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);
        
        modelBuilder.Entity<StockTaking>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30);
        
        modelBuilder.Entity<StockTakingDetail>()
            .Property(x => x.Action)
            .HasConversion<string>()
            .HasMaxLength(30);

        //FOR UNIQUE INDEX
        modelBuilder.Entity<Tag>()
            .HasIndex(x => x.TagId)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.UserId)
            .IsUnique();

        modelBuilder.Entity<DODetailTag>()
            .HasIndex(u => u.TagId)
            .IsUnique();


        //FOR RELATION
        modelBuilder.Entity<User_Role>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<User_Role>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Role_Permission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Role_Permission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Transaction_Detail>()
            .HasOne(td => td.Transaction)
            .WithMany(t => t.TransactionDetails)
            .HasForeignKey(td => td.TrsId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Transaction_Detail>()
            .HasOne(td => td.Tag)
            .WithMany(t => t.TransactionDetails)
            .HasForeignKey(td => td.TagId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Transaction_Detail>()
            .HasOne(td => td.Item)
            .WithMany(i => i.TransactionDetails)
            .HasForeignKey(td => td.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<StockTakingDetail>()
            .HasOne(d => d.StockTaking)
            .WithMany(h => h.Details)
            .HasForeignKey(d => d.SttId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockTakingDetail>()
            .HasOne(d => d.Tag)
            .WithMany()
            .HasForeignKey(d => d.TagId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTakingDetail>()
            .HasOne(d => d.Item)
            .WithMany()
            .HasForeignKey(d => d.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HistoryPrint>()
            .HasOne(h => h.Item)
            .WithMany()
            .HasForeignKey(h => h.ItemId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Permission>()
            .HasOne(p => p.Module)
            .WithMany(m => m.Permissions)
            .HasForeignKey(p => p.ModuleId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<DODetailTag>()
            .HasOne(x => x.DODetail)
            .WithMany(x => x.DODetailTags)
            .HasForeignKey(x => x.DoDetailId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DODetailTag>()
            .HasOne(x => x.Tag)
            .WithMany(x => x.DODetailTags)
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


