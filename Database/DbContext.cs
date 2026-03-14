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
        public DbSet<ExpenseItemAttachment> ExpenseItemAttachments { get; set; }
        public DbSet<PaymentMethods> PaymentMethods { get; set; }
        public DbSet<ExpenseCategory> ExpenseCategory { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleInspection> VehicleInspection { get; set; } // Assuming VehicleType is a class that represents different types of vehicles
        public DbSet<VehicleInspectionAttachment> VehicleInspectionAttachments { get; set; }
        public DbSet<JobServices> JobServices { get; set; } // Assuming JobServices is a class that represents the relationship between jobs and services
        //public DbSet<PaymentMethods> PaymentMethods { get; set; } // Assuming PaymentMethods is a class that represents payment methods
        //public DbSet<AppUser> AppUsers { get; set; } // Assuming AppUser is a class that represents application users
        //public DbSet<ExpenseItem> Expenses { get; set; } // Assuming Expense is a class that represents expenses
        public DbSet<CompanyDetails> CompanyDetails { get; set; } // Assuming CompanyDetails is a class that represents company details
        
    }
}
