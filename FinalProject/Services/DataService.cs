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
            
            return JsonSerializer.Deserialize<Pet>(json) ?? new Pet();
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
}