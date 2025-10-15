using Microsoft.EntityFrameworkCore;
using Movie_API.Models.Domain; // ✅ QUAN TRỌNG: Đảm bảo using đúng namespace của model

namespace Movie_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> dbContextOptions) : base(dbContextOptions)
        {
        }

        // Định nghĩa mối quan hệ giữa các bảng
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Movie_Actors>()
                .HasOne(m => m.Movie)
                .WithMany(ma => ma.Movie_Actor)
                .HasForeignKey(mi => mi.MovieId);

            modelBuilder.Entity<Movie_Actors>()
                .HasOne(a => a.Actor)
                .WithMany(ma => ma.Movie_Actors)
                .HasForeignKey(ai => ai.ActorId);
        }

        // Khai báo các DbSet
        public DbSet<Movies> Movies { get; set; }
        public DbSet<Actors> Actors { get; set; }
        public DbSet<Studios> Studios { get; set; }
        public DbSet<Movie_Actors> Movie_Actors { get; set; }
        public DbSet<Poster> Posters { get; set; }

        // ✅ SỬA LỖI Ở ĐÂY: Thêm dòng DbSet cho Image
        public DbSet<Image> Images { get; set; }
    }
}
