using System.Xml.Linq;
using Platinum.Domain;
namespace Platinum.Infrastructure;
public sealed class XmlLayoutStore
{
    public void Save(Layout layout, string filePath)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var doc = new XDocument(new XElement("platinum-layout", new XAttribute("format-version", "1"),
            new XElement("blocks", layout.Blocks.Select(b => new XElement("block",
                new XAttribute("id", b.Id), new XAttribute("name", b.Name), new XAttribute("level", b.Level),
                new XAttribute("occupancy", b.Occupancy)))),
            new XElement("routes", layout.Routes.Select(r => new XElement("route",
                new XAttribute("id", r.Id), new XAttribute("name", r.Name),
                r.BlockIds.Select(id => new XElement("block-ref", new XAttribute("id", id))))))));
        var full = Path.GetFullPath(filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var temp = full + ".tmp";
        doc.Save(temp);
        File.Move(temp, full, true);
    }
    public Layout Load(string filePath)
    {
        var root = XDocument.Load(filePath).Root ?? throw new InvalidDataException("XML buit.");
        if (root.Name != "platinum-layout" || (string?)root.Attribute("format-version") != "1")
            throw new InvalidDataException("Versió del projecte no compatible.");
        var layout = new Layout();
        foreach (var e in root.Element("blocks")?.Elements("block") ?? [])
            layout.AddBlock(new TrackBlock(Guid.Parse((string?)e.Attribute("id") ?? ""),
                (string?)e.Attribute("name") ?? "", (int?)e.Attribute("level") ?? 0,
                Enum.Parse<OccupancyState>((string?)e.Attribute("occupancy") ?? "Unknown")));
        foreach (var e in root.Element("routes")?.Elements("route") ?? [])
            layout.AddRoute(new Route(Guid.Parse((string?)e.Attribute("id") ?? ""),
                (string?)e.Attribute("name") ?? "", e.Elements("block-ref")
                    .Select(x => Guid.Parse((string?)x.Attribute("id") ?? "")).ToArray()));
        return layout;
    }
}