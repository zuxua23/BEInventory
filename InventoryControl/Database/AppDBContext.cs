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

    public DbSet<Item> Items { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Reader> Readers { get; set; }

    public DbSet<DO> DOs { get; set; }
    public DbSet<DODetail> DODetails { get; set; }

    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Transaction_Detail> TransactionDetails { get; set; }

    public DbSet<StockTaking> StockTakings { get; set; }
    public DbSet<StockTakingDetail> StockTakingDetails { get; set; }

    public DbSet<History> Histories { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
        .HasQueryFilter(u => !u.IsDelete);

            modelBuilder.Entity<Role>()
        .HasQueryFilter(u => !u.IsDelete);

            modelBuilder.Entity<Permission>()
        .HasQueryFilter(u => !u.IsDelete);



        modelBuilder.Entity<User>()
        .HasIndex(u => u.UserId)
        .IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.RolId)
            .IsUnique();

        modelBuilder.Entity<Permission>()
            .HasIndex(p => p.PerId)
            .IsUnique();

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
            .HasForeignKey(td => td.TransactionId)
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
    }
}


