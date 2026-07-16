using FinTrackAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FinTrackAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Income> Income { get; set; }
        public DbSet<RecurringIncome> RecurringIncomes { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<BudgetTarget> BudgetTargets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Tell EF Core the precision that's already actually in the database,
            // so it stops warning about it on every migration.
            modelBuilder.Entity<Expense>().Property(e => e.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<Income>().Property(i => i.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<RecurringIncome>().Property(r => r.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<ActivityLog>().Property(a => a.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<BudgetTarget>().Property(b => b.Amount).HasPrecision(18, 2);

            // Foreign keys: every UserId must point at a real user, and deleting
            // a user automatically cleans up everything that belonged to them.
            modelBuilder.Entity<Expense>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Income>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecurringIncome>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ActivityLog>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BudgetTarget>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}