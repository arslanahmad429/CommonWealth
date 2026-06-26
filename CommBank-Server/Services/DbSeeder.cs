using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace CommBank.Services;

public static class DbSeeder
{
    public static async Task SeedAsync(IMongoDatabase database)
    {
        Console.WriteLine("Starting database seeding...");

        // Determine paths
        string[] searchPaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "commbank-program", "data"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "commbank-program", "data"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "commbank-program", "data"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "commbank-program", "data")
        };

        string? dataPath = searchPaths.FirstOrDefault(Directory.Exists);

        if (dataPath == null)
        {
            Console.Error.WriteLine("Error: Could not locate 'commbank-program/data' folder.");
            Console.Error.WriteLine("Searched paths:");
            foreach (var path in searchPaths)
            {
                Console.Error.WriteLine($"  - {path}");
            }
            return;
        }

        Console.WriteLine($"Found data folder at: {dataPath}");

        var files = new[]
        {
            ("Accounts.json", "Accounts"),
            ("Goals.json", "Goals"),
            ("Tags.json", "Tags"),
            ("Transactions.json", "Transactions"),
            ("Users.json", "Users")
        };

        foreach (var (filename, collectionName) in files)
        {
            string filePath = Path.Combine(dataPath, filename);
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Warning: File {filename} not found at {filePath}. Skipping.");
                continue;
            }

            Console.WriteLine($"Seeding collection '{collectionName}' from {filename}...");

            string jsonContent = await File.ReadAllTextAsync(filePath);
            
            // Parse BSON Array from JSON content
            BsonArray bsonArray;
            try
            {
                bsonArray = BsonSerializer.Deserialize<BsonArray>(jsonContent);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error parsing JSON from {filename}: {ex.Message}");
                continue;
            }

            var collection = database.GetCollection<BsonDocument>(collectionName);

            // Clear existing data
            await collection.DeleteManyAsync(new BsonDocument());

            if (bsonArray.Count > 0)
            {
                var documents = bsonArray.Select(val => val.AsBsonDocument).ToList();
                await collection.InsertManyAsync(documents);
                Console.WriteLine($"Successfully seeded {documents.Count} documents into '{collectionName}'.");
            }
            else
            {
                Console.WriteLine($"Collection '{collectionName}' is empty in source file.");
            }
        }

        Console.WriteLine("Database seeding completed successfully.");
    }
}
