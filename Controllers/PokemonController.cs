using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("v1/learnpoke")]

public class PokemonController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PokemonController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri("https://pokeapi.co/api/v2/");
    }
//detail'd information
    [HttpGet("{pokemonName}/details")]
    [Authorize]//specific middleware
    public async Task<IActionResult> GetPokemonDetails(string pokemonName)
    {
        try
        {
            var response = await _httpClient.GetAsync($"pokemon/{pokemonName.ToLower()}");
            
            if (!response.IsSuccessStatusCode)
            {
                return NotFound("Pokemon not found");
            }

            var content = await response.Content.ReadAsStringAsync();
            var pokemonData = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);

            var speciesResponse = await _httpClient.GetAsync($"pokemon-species/{pokemonName.ToLower()}");
            if (!speciesResponse.IsSuccessStatusCode)
            {
                return NotFound("Pokemon species info not found");
            }

            var speciesContent = await speciesResponse.Content.ReadAsStringAsync();
            var speciesData = JsonSerializer.Deserialize<JsonElement>(speciesContent, _jsonOptions);

            string description = "No description available";
            if (speciesData.TryGetProperty("flavor_text_entries", out var flavorTextEntries))
            {
                foreach (var entry in flavorTextEntries.EnumerateArray())
                {
                    if (entry.TryGetProperty("language", out var language) &&
                        language.TryGetProperty("name", out var langName) &&
                        langName.GetString() == "en" &&
                        entry.TryGetProperty("flavor_text", out var flavorText))
                    {
                        description = flavorText.GetString() ?? description;
                        break;
                    }
                }
            }

            // Get image URL
            string? imageUrl = null;
            if (pokemonData.TryGetProperty("sprites", out var sprites))
            {
                if (sprites.TryGetProperty("other", out var other) &&
                    other.TryGetProperty("official-artwork", out var officialArtwork) &&
                    officialArtwork.TryGetProperty("front_default", out var officialImage))
                {
                    imageUrl = officialImage.GetString();
                }
                else if (sprites.TryGetProperty("front_default", out var defaultImage))
                {
                    imageUrl = defaultImage.GetString();
                }
            }

            // response
            var result = new
            {
                Name = pokemonName,
                Image = imageUrl ?? string.Empty, 
                Description = description
            };

            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(500, $"Error accessing PokeAPI: {ex.Message}");
        }
    }


//translation
[HttpGet("{pokemonName}/translate")]
[Authorize]
public async Task<IActionResult> GetPokemonTranslations(string pokemonName)
{
    if (string.IsNullOrWhiteSpace(pokemonName))
    {
        return BadRequest("Pokemon name cannot be empty");
    }

    try
    {
        var speciesResponse = await _httpClient.GetAsync($"pokemon-species/{pokemonName.ToLower()}");
        
        if (!speciesResponse.IsSuccessStatusCode)
        {
            return NotFound("Pokemon species info not found");
        }

        var speciesContent = await speciesResponse.Content.ReadAsStringAsync();
        var speciesData = JsonSerializer.Deserialize<JsonElement>(speciesContent, _jsonOptions);

        // default English 
        var translations = new Dictionary<string, string>
        {
            ["English"] = pokemonName
        };

        // getin english name
        if (speciesData.TryGetProperty("name", out var englishNameProp) && 
            englishNameProp.ValueKind != JsonValueKind.Null)
        {
            var englishName = englishNameProp.GetString();
            if (!string.IsNullOrEmpty(englishName))
            {
                translations["English"] = englishName;
            }
        }

        if (speciesData.TryGetProperty("names", out var names) && 
            names.ValueKind == JsonValueKind.Array)
        {
            foreach (var nameEntry in names.EnumerateArray())
            {
                if (nameEntry.ValueKind != JsonValueKind.Object) continue;

                if (nameEntry.TryGetProperty("language", out var language) &&
                    language.ValueKind == JsonValueKind.Object &&
                    language.TryGetProperty("name", out var langCode) &&
                    langCode.ValueKind == JsonValueKind.String &&
                    nameEntry.TryGetProperty("name", out var name) &&
                    name.ValueKind == JsonValueKind.String)
                {
                    var langCodeValue = langCode.GetString();
                    var nameValue = name.GetString();

                    if (string.IsNullOrEmpty(langCodeValue)) continue;
                    if (string.IsNullOrEmpty(nameValue)) continue;

                    switch (langCodeValue)
                    {
                        case "ja": // Katakana
                            translations["Japanese"] = nameValue;
                            break;
                        case "fr": // French
                            translations["French"] = nameValue;
                            break;
                        case "de": // German
                            translations["German"] = nameValue;
                            break;
                    }
                }
            }
        }

        // Ensure we have all expected languages (even if empty)
        if (!translations.ContainsKey("Japanese")) translations["Japanese"] = "Not available";
        if (!translations.ContainsKey("French")) translations["French"] = "Not available";
        if (!translations.ContainsKey("German")) translations["German"] = "Not available";

        return Ok(translations);
    }
    catch (HttpRequestException ex)
    {
        return StatusCode(500, $"Error accessing PokeAPI: {ex.Message}");
    }
    catch (JsonException ex)
    {
        return StatusCode(500, $"Error parsing PokeAPI response: {ex.Message}");
    }
    catch (Exception ex)
    {
        return StatusCode(500, $"Unexpected error: {ex.Message}");
    }
}
}