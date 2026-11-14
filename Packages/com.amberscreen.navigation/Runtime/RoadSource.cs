using System.Collections.Generic;
using UnityEngine;

namespace AmberScreen.Navigation
{
    /// <summary>
    /// Supplies road centre lines for reset-to-road. The game adapts its own road tool to this;
    /// the package never reads a road or vehicle asset.
    /// </summary>
    public interface IRoadSource
    {
        IReadOnlyList<RoadPolyline> Roads { get; }
    }

    /// <summary>
    /// One road centre line as a <see cref="RoutePath"/>. When <see cref="TwoWay"/> is false the
    /// points must run in the only allowed driving direction.
    /// </summary>
    public sealed class RoadPolyline
    {
        public RoadPolyline(IReadOnlyList<Vector3> worldPoints, bool twoWay = true)
        {
            Path = new RoutePath();
            Path.SetPoints(worldPoints);
            TwoWay = twoWay;
        }

        public RoutePath Path { get; }
        public bool TwoWay { get; set; }
    }

    /// <summary>Simple list-backed <see cref="IRoadSource"/> for adapters, tests and demos.</summary>
    public sealed class RoadSourceList : IRoadSource
    {
        private readonly List<RoadPolyline> roads = new List<RoadPolyline>();

        public IReadOnlyList<RoadPolyline> Roads => roads;

        public RoadPolyline Add(IReadOnlyList<Vector3> worldPoints, bool twoWay = true)
        {
            var road = new RoadPolyline(worldPoints, twoWay);
            roads.Add(road);
            return road;
        }

        public void Add(RoadPolyline road)
        {
            if (road != null) roads.Add(road);
        }

        public void Clear() => roads.Clear();
    }
}