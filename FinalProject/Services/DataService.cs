using System.Collections.Generic;
using System.Text.Json;
using FinalProject.Models;

namespace FinalProject.Services;

public class DataService
{
    private const string PetKey = "pet_data";
    private const string PetKeyPrefs = "pet_data_prefs";

    public async Task<Pet> LoadPetAsync()
    {
        string? json = null;

        try
        {
            json = await SecureStorage.GetAsync(PetKey); //acilista peti oku
        }
        catch { /* ignore */ }

        if (string.IsNullOrWhiteSpace(json))
            json = Preferences.Get(PetKeyPrefs, null);

        if (string.IsNullOrWhiteSpace(json))
            return new Pet();

        try
        {
            var pet = JsonSerializer.Deserialize<Pet>(json) ?? new Pet();
            EnsureCollections(pet);
            return pet;
        }
        catch
        {
            return new Pet();
        }
    }

    public async Task SavePetAsync(Pet pet) //History, coin, stat vs kaydi 
    {
        EnsureCollections(pet);
        var json = JsonSerializer.Serialize(pet); //peti kaydet

        Preferences.Set(PetKeyPrefs, json);

        try
        {
            await SecureStorage.SetAsync(PetKey, json); //Petin durumunu JSON olarka kaydet
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SecureStorage save error: {ex.Message}");
        }
    }

    public Task ResetDataAsync()
    {
        SecureStorage.Remove(PetKey);
        return Task.CompletedTask;
    }

    
    private static void EnsureCollections(Pet pet)
    {
        pet.OwnedItemIds ??= new List<string>();
        pet.FocusSessions ??= new List<FocusSession>();
    }
}