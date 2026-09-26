using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FindIt.Models;

namespace FindIt.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure database is created and migrations applied
        await context.Database.MigrateAsync();

        // 1. Seed Roles
        string[] roles = ["Admin", "User"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Seed Users
        var adminUser = await userManager.FindByEmailAsync("admin@findit.com");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@findit.com",
                Email = "admin@findit.com",
                EmailConfirmed = true,
                FullName = "Admin User",
                DepartmentOrRole = "System Administrator"
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        var demoUser1 = await userManager.FindByEmailAsync("john@findit.com");
        if (demoUser1 == null)
        {
            demoUser1 = new ApplicationUser
            {
                UserName = "john@findit.com",
                Email = "john@findit.com",
                EmailConfirmed = true,
                FullName = "John Doe",
                DepartmentOrRole = "Computer Science"
            };
            var result = await userManager.CreateAsync(demoUser1, "User@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(demoUser1, "User");
            }
        }

        var demoUser2 = await userManager.FindByEmailAsync("jane@findit.com");
        if (demoUser2 == null)
        {
            demoUser2 = new ApplicationUser
            {
                UserName = "jane@findit.com",
                Email = "jane@findit.com",
                EmailConfirmed = true,
                FullName = "Jane Smith",
                DepartmentOrRole = "Business Administration"
            };
            var result = await userManager.CreateAsync(demoUser2, "User@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(demoUser2, "User");
            }
        }

        // 3. Seed Categories
        if (!context.Categories.Any())
        {
            var categories = new List<Category>
            {
                new() { Name = "Electronics", Description = "Laptops, phones, headphones, chargers, tablets" },
                new() { Name = "Wallets & Bags", Description = "Wallets, backpacks, purses, totes, gym bags" },
                new() { Name = "Keys & Keychains", Description = "Car keys, house keys, dorm keys, fobs" },
                new() { Name = "IDs & Documents", Description = "Student IDs, national IDs, driver licenses, passports" },
                new() { Name = "Books & Notebooks", Description = "Textbooks, notebooks, binders, stationery" },
                new() { Name = "Clothing & Accessories", Description = "Jackets, sweaters, sunglasses, watches, jewelry" },
                new() { Name = "Other Items", Description = "Water bottles, umbrellas, miscellaneous" }
            };
            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();
        }

        // 4. Seed Items
        if (!context.Items.Any() && demoUser1 != null && demoUser2 != null)
        {
            var electronicsCat = await context.Categories.FirstAsync(c => c.Name == "Electronics");
            var walletCat = await context.Categories.FirstAsync(c => c.Name == "Wallets & Bags");
            var keysCat = await context.Categories.FirstAsync(c => c.Name == "Keys & Keychains");
            var idsCat = await context.Categories.FirstAsync(c => c.Name == "IDs & Documents");

            var items = new List<Item>
            {
                new()
                {
                    Title = "Apple iPhone 14 Pro (Space Black)",
                    Description = "Found near table 14 in Central Library 2nd floor. It has a matte black protective case with slight scratch on bottom right.",
                    Type = ItemType.Found,
                    CategoryId = electronicsCat.Id,
                    Location = "Central Library, 2nd Floor",
                    DateLostOrFound = DateTime.Today.AddDays(-2),
                    SecretIdentifier = "What photo is displayed on the phone lock screen?",
                    Status = ItemStatus.Open,
                    UserId = demoUser1.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new()
                {
                    Title = "Brown Leather Fossil Wallet",
                    Description = "Lost my brown bi-fold Fossil wallet after lunch. Contains my student ID and debit cards. Urgent!",
                    Type = ItemType.Lost,
                    CategoryId = walletCat.Id,
                    Location = "Campus Cafeteria, Table 8",
                    DateLostOrFound = DateTime.Today.AddDays(-3),
                    Status = ItemStatus.Open,
                    UserId = demoUser2.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new()
                {
                    Title = "Toyota Car Key with Blue Lanyard",
                    Description = "Toyota smart key fob attached to a blue CSE department lanyard. Handed to security reception.",
                    Type = ItemType.Found,
                    CategoryId = keysCat.Id,
                    Location = "Student Parking Lot B",
                    DateLostOrFound = DateTime.Today.AddDays(-1),
                    SecretIdentifier = "What specific metallic charm or miniature toy is attached to the ring?",
                    Status = ItemStatus.Open,
                    UserId = demoUser1.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new()
                {
                    Title = "Student ID Card - Jane Smith",
                    Description = "Lost my university ID card somewhere between the Science Building and Cafeteria.",
                    Type = ItemType.Lost,
                    CategoryId = idsCat.Id,
                    Location = "Science Building Walkway",
                    DateLostOrFound = DateTime.Today.AddDays(-4),
                    Status = ItemStatus.Claimed,
                    UserId = demoUser2.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                },
                new()
                {
                    Title = "Sony WH-1000XM4 Noise Canceling Headphones",
                    Description = "Black over-ear Sony headphones inside gray zippered travel case. Left in study room C.",
                    Type = ItemType.Found,
                    CategoryId = electronicsCat.Id,
                    Location = "Study Room C, Student Union",
                    DateLostOrFound = DateTime.Today,
                    SecretIdentifier = "What sticker is on the hard case?",
                    Status = ItemStatus.Open,
                    UserId = demoUser1.Id,
                    CreatedAt = DateTime.UtcNow
                }
            };
            context.Items.AddRange(items);
            await context.SaveChangesAsync();

            // 5. Seed a Sample Claim
            var phoneItem = items[0];
            var sampleClaim = new Claim
            {
                ItemId = phoneItem.Id,
                ClaimantId = demoUser2.Id,
                SecretAnswer = "The lockscreen wallpaper is a photo of my golden retriever on the beach.",
                ProofDescription = "I bought this iPhone at the university store in October. I have the digital invoice and can unlock with FaceID.",
                Status = ClaimStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddHours(-12)
            };
            context.Claims.Add(sampleClaim);
            await context.SaveChangesAsync();

            // 6. Seed Sample Messages
            var messages = new List<Message>
            {
                new()
                {
                    SenderId = demoUser2.Id,
                    ReceiverId = demoUser1.Id,
                    ItemId = phoneItem.Id,
                    Content = "Hello John, I saw your post about the iPhone 14 found in the library. I submitted a claim with the lock screen description!",
                    SentAt = DateTime.UtcNow.AddHours(-10),
                    IsRead = true
                },
                new()
                {
                    SenderId = demoUser1.Id,
                    ReceiverId = demoUser2.Id,
                    ItemId = phoneItem.Id,
                    Content = "Hi Jane! That matches the photo exactly! I gave the phone to the Library Help Desk, you can claim it with your ID.",
                    SentAt = DateTime.UtcNow.AddHours(-8),
                    IsRead = false
                }
            };
            context.Messages.AddRange(messages);
            await context.SaveChangesAsync();
        }
    }
}
