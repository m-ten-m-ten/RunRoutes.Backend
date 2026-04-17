using System.Xml.Linq;
using NetTopologySuite.Geometries;

namespace RunRoutes.Core.Helpers;

public static class GpxParser
{
    private static readonly XNamespace GpxNs = "http://www.topografix.com/GPX/1/1";

    public static LineString Parse(string gpxXml)
    {
        var doc = XDocument.Parse(gpxXml);

        var coordinates = doc.Descendants(GpxNs + "trkpt")
            .Select(pt => new Coordinate(
                (double)pt.Attribute("lon")!,
                (double)pt.Attribute("lat")!
            ))
            .ToArray();

        if (coordinates.Length < 2)
            throw new ArgumentException("GPXトラックには2点以上の座標が必要です");

        return new LineString(coordinates) { SRID = 4326 };
    }
}
