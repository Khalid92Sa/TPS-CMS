using CMS.Domain.DataSeeds;
using CMS.Domain.Entities;
using CMS.Domain.EntityMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace CMS.Domain
{   
    public class ApplicationDbContext: IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<Company> Companies { get; set; }
        public DbSet<CarrerOffer> CarrerOffers { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<Interviews> Interviews { get; set; }
        public DbSet<Notifications> Notifications { get; set; }
        public DbSet<Templates> Templates { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<Track> Tracks { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfiguration(new CompanyMapper());
            builder.ApplyConfiguration(new CountryMapper());
            builder.ApplyConfiguration(new CandidateMapper());
            builder.ApplyConfiguration(new InterviewsMapper());
            builder.ApplyConfiguration(new CarrerOfferMapper());
            

            var adminRole = new IdentityRole { Id= "b024cbbe-f64e-4d1b-9c6e-05ac0f0e3ebb", Name = "Admin", NormalizedName = "Admin".ToUpper() };
            builder.Entity<IdentityRole>().HasData(
                adminRole,
                new IdentityRole { Id = "1eecb40c-c701-4445-89d4-d1aa7d70460d", Name = "General Manager", NormalizedName = "General Manager".ToUpper() },
                new IdentityRole { Id = "226cca69-f046-4d15-8b81-9b9ba34f2214", Name = "HR Manager", NormalizedName = "HR ManagerF".ToUpper() },
                new IdentityRole { Id = "91c3461a-7da3-4033-b907-b104b903d793",  Name = "Interviewer", NormalizedName = "Interviewer".ToUpper() },
                new IdentityRole { Id = "3F476A40-97F4-42C6-A226-602AED74A4BC", Name = "Solution Architecture", NormalizedName = "Solution Architecture".ToUpper() }

            );
            
            var adminUser = new IdentityUser { Id= "c6585ab9-8b5f-4332-a174-92429db8add2", UserName = "admin", NormalizedUserName= "admin".ToUpper(), Email = "admin@admin.com", NormalizedEmail = "admin@admin.com".ToUpper(), EmailConfirmed = true };
            PasswordHasher<IdentityUser> hasher = new PasswordHasher<IdentityUser>();
            var hashedPassword = hasher.HashPassword(adminUser,"admin123");
            adminUser.PasswordHash = hashedPassword;
            
            builder.Entity<IdentityUser>().HasData(adminUser);
            builder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string> { RoleId = adminRole.Id, UserId= adminUser.Id });
            builder.ApplyConfiguration(new PositionMapper());  
            builder.ApplyConfiguration(new StatusMapper());
            builder.ApplyConfiguration(new StatusSeed());
            builder.ApplyConfiguration(new TrackMapper());
            builder.ApplyConfiguration(new TrackSeed());
            base.OnModelCreating(builder);
        }
    }
}
