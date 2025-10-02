using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;

public class Program
{
    public static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var sqlConnectionString = configuration.GetConnectionString("DefaultConnection");
        var firebaseProjectId = configuration["Firebase:ProjectId"];
        var googleCredentialsJsonPath = configuration["Firebase:GoogleCredentialsJson"];

        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", googleCredentialsJsonPath);

        Console.WriteLine("Starting data migration...");

        try
        {
            var db = await FirestoreDb.CreateAsync(firebaseProjectId);
            Console.WriteLine("Successfully connected to Firestore.");

            using (var connection = new SqlConnection(sqlConnectionString))
            {
                await connection.OpenAsync();
                Console.WriteLine("Successfully connected to SQL Server.");

                var tablesToMigrate = new List<string>
                {
                    "Colaboradores", "Computadores", "Monitores",
                    "Perifericos", "Manutencoes", "Chamados", "Rede"
                };

                foreach (var tableName in tablesToMigrate)
                {
                    await MigrateTableAsync(connection, db, tableName);
                }
            }

            Console.WriteLine("Data migration completed successfully!");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"An error occurred during migration: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
        }
    }

    private static async Task MigrateTableAsync(SqlConnection connection, FirestoreDb db, string tableName)
    {
        Console.WriteLine($"\nMigrating table: {tableName}...");
        var collectionRef = db.Collection(tableName.ToLower());
        int count = 0;

        using (var command = new SqlCommand($"SELECT * FROM {tableName}", connection))
        using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var data = new Dictionary<string, object>();
                string primaryKeyField = null;
                object primaryKeyValue = null;

                // Attempt to get primary key from schema
                try
                {
                    var schema = await reader.GetSchemaTableAsync();
                    foreach (DataRow row in schema.Rows)
                    {
                        if ((bool)row["IsKey"])
                        {
                            primaryKeyField = row["ColumnName"].ToString();
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                     Console.WriteLine($"Could not get schema for table {tableName}. Will use first column as document Id. Error: {ex.Message}");
                }


                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.GetValue(i);

                    // Convert DBNull to null for Firestore
                    data[columnName] = value == DBNull.Value ? null : value;

                    // Use the identified primary key value for the document ID
                    if (columnName.Equals(primaryKeyField, StringComparison.OrdinalIgnoreCase))
                    {
                        primaryKeyValue = value;
                    }
                }

                if (primaryKeyValue != null)
                {
                    string documentId = primaryKeyValue.ToString().Replace("/", "-"); // Firestore doesn't like slashes in IDs
                    await collectionRef.Document(documentId).SetAsync(data);
                    count++;
                }
                else
                {
                    // Fallback if primary key is not found or is null
                    await collectionRef.AddAsync(data);
                    count++;
                }
            }
        }
        Console.WriteLine($"Migrated {count} documents to collection '{tableName.ToLower()}'.");
    }
}