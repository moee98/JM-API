using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using JMAPI.Models;
using System;

namespace JMAPI.Database
{


    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
        }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ExpenseItem> ExpenseItems { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<UserRole> UserRoles { get; set; } // Assuming UserRole is an enum or a class that represents user roles
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleInspection> VehicleInspection { get; set; } // Assuming VehicleType is a class that represents different types of vehicles
        public DbSet<JobServices> JobServices { get; set; } // Assuming JobServices is a class that represents the relationship between jobs and services
         
    }
}
