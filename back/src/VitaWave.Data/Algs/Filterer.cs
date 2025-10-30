using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VitaWave.Common;
using VitaWave.Common.TLVs;

namespace VitaWave.Data.Algs
{
    public class Filterer
    {
        
        public double Eps { get; set; } = 0.5;
        public int MinPts { get; set; } = 15;

        public List<FilteredPerson> Filter(EventPacket e)
        {
            // For now, all this does is grab all the info out of the packet for each TID :)
            // TODO: Implement fancy stuff

            return DoBadWay(e);
        }

        public void ChangeDBParams(double eps, int minPts)
        {
            this.Eps = eps;
            this.MinPts = minPts;
        }

        private List<FilteredPerson> DoBadWay(EventPacket e)
        {
            var list = new List<FilteredPerson>(e.Targets.Count);

            try
            {
                for (int i = 0; i < e.Targets.Count; i++)
                {
                    if (e.Targets[i].TID >= 253) // Unknown TID according to TI documentation
                        continue;

                    list[i] = new FilteredPerson();
                    list[i].TID = e.Targets[i].TID.ToString();
                    list[i].TargetHeight = e.TargetHeights[i];
                    list[i].Points = e.Points.Where(p => p.TID == e.Targets[i].TID).ToList();
                    list[i].TimeSinceLastMs = e.TimeSinceLastMs;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal("Exception in Filterer: " + ex.Message);
            }

            return list;
        }

        public List<List<PointCloudPoint>> Fit(List<PointCloudPoint> points)
        {
            var clusterId = 0;
            foreach (var p in points)
            {
                p.Visited = true;

                var neighbors = RegionQuery(points, p);
                if (neighbors.Count < MinPts)
                {
                    p.ClusterId = 0; // mark as noise
                }
                else
                {
                    clusterId++;
                    ExpandCluster(points, p, neighbors, clusterId);
                }
            }

            return points
                .Where(p => p.ClusterId > 0)
                .GroupBy(p => p.ClusterId)
                .Select(g => g.ToList())
                .ToList();
        }

        private void ExpandCluster(List<PointCloudPoint> points, PointCloudPoint p, List<PointCloudPoint> neighbors, int clusterId)
        {
            p.ClusterId = clusterId;
            for (int i = 0; i < neighbors.Count; i++)
            {
                var n = neighbors[i];
                if (!n.Visited)
                {
                    n.Visited = true;
                    var nNeighbors = RegionQuery(points, n);
                    if (nNeighbors.Count >= MinPts)
                        neighbors.AddRange(nNeighbors);
                }
                if (n.ClusterId <= 0)
                    n.ClusterId = clusterId;
            }
        }

        private List<PointCloudPoint> RegionQuery(List<PointCloudPoint> points, PointCloudPoint p)
        {
            return points.Where(q =>
            {
                double dx = q.X - p.X;
                double dz = q.Z - p.Z;
                return dx * dx + dz * dz <= Eps * Eps;
            }).ToList();
        }
    }
}