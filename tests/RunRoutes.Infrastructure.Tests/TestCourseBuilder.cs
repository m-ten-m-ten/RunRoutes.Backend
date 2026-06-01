using NetTopologySuite.Geometries;

namespace RunRoutes.Infrastructure.Tests;

internal static class TestCourseBuilder
{
    public static LineString CreateTestRoute()
    {
        var factory = new GeometryFactory(new PrecisionModel(), 4326);

        // 札幌駅付近の2点（SRID 4326）
        return factory.CreateLineString([
            new Coordinate(141.3507, 43.0686),
            new Coordinate(141.3522, 43.0700),
        ]);
    }

    public static LineString CreateTestRoute(double lng, double lat)
    {
        var factory = new GeometryFactory(new PrecisionModel(), 4326);
        return factory.CreateLineString([
            new Coordinate(lng, lat),
            new Coordinate(lng + 0.001, lat + 0.001),
        ]);
    }
}