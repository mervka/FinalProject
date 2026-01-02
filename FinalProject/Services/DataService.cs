using System.Collections.Generic;
using System.Text.Json;
using FinalProject.Models;

namespace FinalProject.Services;

public class DataService
{
    private const string PetKey = "pet_data";

    public async Task<Pet> LoadPetAsync()
    {
        try
        {
            var json = await SecureStorage.GetAsync(PetKey);
            
            if (string.IsNullOrEmpty(json))
            {
                return new Pet();
            }
            
            //return JsonSerializer.Deserialize<Pet>(json) ?? new Pet();
            var pet = JsonSerializer.Deserialize<Pet>(json) ?? new Pet();
            EnsureCollections(pet);
            return pet;
        }
        catch
        {
            return new Pet();
        }
    }

    public async Task SavePetAsync(Pet pet)
    {
        try
        {
            EnsureCollections(pet);
            var json = JsonSerializer.Serialize(pet);
            await SecureStorage.SetAsync(PetKey, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save error: {ex.Message}");
        }
    }

    public async Task ResetDataAsync()
    {
        SecureStorage.Remove(PetKey);
        await Task.CompletedTask;
    }
    
    // Keeps deserialized lists non-null for clean binding and saving.
    private static void EnsureCollections(Pet pet)
    {
        pet.OwnedItemIds ??= new List<string>();
        pet.RoomItems ??= new List<RoomItem>();
        pet.FocusSessions ??= new List<FocusSession>();
    }
}