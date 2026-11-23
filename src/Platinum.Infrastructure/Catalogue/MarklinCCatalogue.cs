using System.IO;
using System.Text.Json;
using Platinum.Domain.Catalogue;

namespace Platinum.Infrastructure.Catalogue;

public static class MarklinCCatalogue
{
    public static CatalogueDocument Load(string path)
    {
        using var stream = File.OpenRead(path);
        var document = JsonSerializer.Deserialize<CatalogueDocument>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return document ?? throw new InvalidDataException("El catàleg Märklin C és buit o no té un format vàlid.");
    }
}
