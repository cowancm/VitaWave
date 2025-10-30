using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using VitaWave.Common;
using VitaWave.Common.TLVs;

namespace VitaWave.Data.Algs
{
    public static class Filterer
    {
        // Distance parameters
        public static double MinDistance { get; set; } = 2.0;
        public static double MaxDistance { get; set; } = 10.0;

        // Quality thresholds
        public static double MinSNR { get; set; } = 10.0; // dB
        public static double MaxDoppler { get; set; } = 3.0; // m/s (normal walking ~1.4 m/s)
        public static double MinDoppler { get; set; } = 0.05; // m/s (filter stationary clutter)

        // Clustering parameters
        public static int MinClusterSize { get; set; } = 10;
        public static int MinSamples { get; set; } = 5;

        // Person validation parameters
        public static double MinPersonHeight { get; set; } = 1.2;
        public static double MaxPersonHeight { get; set; } = 2.5;
        public static double MinHeightSpread { get; set; } = 0.3;
        public static double MaxPersonWidth { get; set; } = 1.5;

        public static List<FilteredPerson> Filter(EventPacket e)
        {
            return DoBadWay(e);
        }

        private static List<FilteredPerson> DoBadWay(EventPacket e)
        {
            var list = new List<FilteredPerson>(e.Targets.Count);
            try
            {
                for (int i = 0; i < e.Targets.Count; i++)
                {
                    if (e.Targets[i].TID >= 253)
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

        public static List<PersonCluster> Fit(List<PointCloudPoint> points)
        {
            // Step 1: Pre-filter based on SNR, Doppler, distance
            var filteredPoints = PreFilterPoints(points);

            if (filteredPoints.Count < MinClusterSize)
            {
                Log.Warning($"Insufficient points after filtering: {filteredPoints.Count}");
                return new List<PersonCluster>();
            }

            // Step 2: Run HDBSCAN
            var clusters = RunHDBSCAN(filteredPoints);

            // Step 3: Post-process and validate clusters
            var personClusters = clusters
                .Where(c => IsValidPersonCluster(c))
                .Select(c => CreatePersonCluster(c))
                .OrderByDescending(c => c.Confidence)
                .ToList();

            Log.Information($"Found {personClusters.Count} valid person clusters from {filteredPoints.Count} points");

            return personClusters;
        }

        private static List<PointCloudPoint> PreFilterPoints(List<PointCloudPoint> points)
        {
            return points.Where(p =>
            {
                // Calculate 3D distance from radar (wall-mounted sensor)
                double distance = Math.Sqrt(p.X * p.X + p.Y * p.Y + p.Z * p.Z);

                // Filter by distance (dead zone and max range)
                if (distance < MinDistance || distance > MaxDistance)
                    return false;

                // Filter by SNR
                if (p.SNR < MinSNR)
                    return false;

                // Filter by Doppler (remove stationary clutter and unrealistic speeds)
                double absDoppler = Math.Abs(p.Doppler);
                if (absDoppler < MinDoppler || absDoppler > MaxDoppler)
                    return false;

                // Filter by height (rough person height range)
                if (p.Y < MinPersonHeight || p.Y > MaxPersonHeight)
                    return false;

                return true;
            }).ToList();
        }

        private static List<List<PointCloudPoint>> RunHDBSCAN(List<PointCloudPoint> points)
        {
            // Calculate core distances for each point
            var coreDistances = CalculateCoreDistances(points);

            // Build mutual reachability distance graph
            var mst = BuildMinimumSpanningTree(points, coreDistances);

            // Extract clusters from hierarchy
            var clusters = ExtractClusters(mst, points);

            return clusters;
        }

        private static Dictionary<PointCloudPoint, double> CalculateCoreDistances(List<PointCloudPoint> points)
        {
            var coreDistances = new Dictionary<PointCloudPoint, double>();

            foreach (var p in points)
            {
                var distances = points
                    .Where(q => q != p)
                    .Select(q => CalculateWeightedDistance(p, q))
                    .OrderBy(d => d)
                    .ToList();

                // Core distance is distance to kth nearest neighbor
                coreDistances[p] = distances.Count >= MinSamples
                    ? distances[MinSamples - 1]
                    : distances.LastOrDefault();
            }

            return coreDistances;
        }

        private static double CalculateWeightedDistance(PointCloudPoint p1, PointCloudPoint p2)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            double dz = p1.Z - p2.Z;

            // Weight height less than horizontal distance
            double horizDist = Math.Sqrt(dx * dx + dz * dz);
            double vertDist = Math.Abs(dy) * 0.5; // Height matters less

            // Also consider SNR and Doppler similarity
            double snrDiff = Math.Abs(p1.SNR - p2.SNR) / 50.0; // Normalize
            double dopplerDiff = Math.Abs(p1.Doppler - p2.Doppler) / 2.0; // Normalize

            return Math.Sqrt(horizDist * horizDist + vertDist * vertDist +
                           snrDiff * snrDiff * 0.1 + dopplerDiff * dopplerDiff * 0.1);
        }

        private static List<Edge> BuildMinimumSpanningTree(List<PointCloudPoint> points,
            Dictionary<PointCloudPoint, double> coreDistances)
        {
            var edges = new List<Edge>();

            // Create all edges with mutual reachability distance
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = i + 1; j < points.Count; j++)
                {
                    var p1 = points[i];
                    var p2 = points[j];

                    double dist = CalculateWeightedDistance(p1, p2);
                    double mutualReachDist = Math.Max(dist,
                        Math.Max(coreDistances[p1], coreDistances[p2]));

                    edges.Add(new Edge { P1 = p1, P2 = p2, Weight = mutualReachDist });
                }
            }

            // Sort edges by weight
            edges = edges.OrderBy(e => e.Weight).ToList();

            // Build MST using Kruskal's algorithm
            var mst = new List<Edge>();
            var unionFind = new UnionFind(points);

            foreach (var edge in edges)
            {
                if (unionFind.Find(edge.P1) != unionFind.Find(edge.P2))
                {
                    unionFind.Union(edge.P1, edge.P2);
                    mst.Add(edge);
                }
            }

            return mst;
        }

        private static List<List<PointCloudPoint>> ExtractClusters(List<Edge> mst, List<PointCloudPoint> points)
        {
            // Sort MST edges by weight (descending)
            var sortedEdges = mst.OrderByDescending(e => e.Weight).ToList();

            var unionFind = new UnionFind(points);

            // Build hierarchy by removing edges from largest to smallest
            foreach (var edge in sortedEdges)
            {
                unionFind.Union(edge.P1, edge.P2);
            }

            // Extract clusters
            var clusterDict = new Dictionary<PointCloudPoint, List<PointCloudPoint>>();

            foreach (var point in points)
            {
                var root = unionFind.Find(point);
                if (!clusterDict.ContainsKey(root))
                    clusterDict[root] = new List<PointCloudPoint>();
                clusterDict[root].Add(point);
            }

            // Filter by minimum cluster size
            return clusterDict.Values
                .Where(c => c.Count >= MinClusterSize)
                .ToList();
        }

        private static bool IsValidPersonCluster(List<PointCloudPoint> cluster)
        {
            if (cluster.Count < MinClusterSize)
                return false;

            // Use median for robustness
            var sortedY = cluster.Select(p => p.Y).OrderBy(y => y).ToList();
            double medianY = GetMedian(sortedY);

            double minY = cluster.Min(p => p.Y);
            double maxY = cluster.Max(p => p.Y);
            double heightSpread = maxY - minY;

            // Height validation
            if (heightSpread < MinHeightSpread)
                return false;

            if (medianY < MinPersonHeight || medianY > MaxPersonHeight)
                return false;

            // Width validation
            double minX = cluster.Min(p => p.X);
            double maxX = cluster.Max(p => p.X);
            double minZ = cluster.Min(p => p.Z);
            double maxZ = cluster.Max(p => p.Z);

            double widthX = maxX - minX;
            double widthZ = maxZ - minZ;

            if (widthX > MaxPersonWidth || widthZ > MaxPersonWidth)
                return false;

            // Check that cluster has reasonable SNR distribution
            double avgSNR = cluster.Average(p => p.SNR);
            if (avgSNR < MinSNR + 5) // Require some margin above minimum
                return false;

            return true;
        }

        private static PersonCluster CreatePersonCluster(List<PointCloudPoint> cluster)
        {
            // Use median for position (robust to outliers)
            var sortedX = cluster.Select(p => p.X).OrderBy(x => x).ToList();
            var sortedY = cluster.Select(p => p.Y).OrderBy(y => y).ToList();
            var sortedZ = cluster.Select(p => p.Z).OrderBy(z => z).ToList();

            double medianX = GetMedian(sortedX);
            double medianY = GetMedian(sortedY);
            double medianZ = GetMedian(sortedZ);

            // Calculate confidence score
            double confidence = CalculateConfidence(cluster, medianX, medianY, medianZ);

            return new PersonCluster
            {
                Points = cluster,
                CenterX = medianX,
                CenterY = medianY,
                CenterZ = medianZ,
                Height = cluster.Max(p => p.Y) - cluster.Min(p => p.Y),
                AvgSNR = cluster.Average(p => p.SNR),
                MedianDoppler = GetMedian(cluster.Select(p => p.Doppler).OrderBy(d => d).ToList()),
                PointCount = cluster.Count,
                Confidence = confidence
            };
        }

        private static double CalculateConfidence(List<PointCloudPoint> cluster,
            double medianX, double medianY, double medianZ)
        {
            double confidence = 0.0;

            // Factor 1: Point count (more points = more confident)
            double pointScore = Math.Min(cluster.Count / 30.0, 1.0) * 25;
            confidence += pointScore;

            // Factor 2: Average SNR (higher SNR = more confident)
            double avgSNR = cluster.Average(p => p.SNR);
            double snrScore = Math.Min((avgSNR - MinSNR) / 20.0, 1.0) * 25;
            confidence += snrScore;

            // Factor 3: Compactness (tighter cluster = more confident)
            double avgDist = cluster.Average(p =>
            {
                double dx = p.X - medianX;
                double dy = p.Y - medianY;
                double dz = p.Z - medianZ;
                return Math.Sqrt(dx * dx + dy * dy + dz * dz);
            });
            double compactScore = Math.Max(0, (1.0 - avgDist / 0.5)) * 20;
            confidence += compactScore;

            // Factor 4: Height spread (good vertical spread = person-like)
            double heightSpread = cluster.Max(p => p.Y) - cluster.Min(p => p.Y);
            double heightScore = Math.Min(heightSpread / 1.5, 1.0) * 15;
            confidence += heightScore;

            // Factor 5: Doppler consistency (similar movement = more likely one person)
            var dopplers = cluster.Select(p => p.Doppler).ToList();
            double dopplerStdDev = CalculateStdDev(dopplers);
            double dopplerScore = Math.Max(0, (1.0 - dopplerStdDev / 0.5)) * 15;
            confidence += dopplerScore;

            return Math.Min(confidence, 100.0);
        }

        private static double GetMedian(List<double> sortedValues)
        {
            int count = sortedValues.Count;
            if (count == 0) return 0;
            if (count % 2 == 1)
                return sortedValues[count / 2];
            return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2.0;
        }

        private static double CalculateStdDev(List<double> values)
        {
            if (values.Count == 0) return 0;
            double avg = values.Average();
            double sumSquares = values.Sum(v => (v - avg) * (v - avg));
            return Math.Sqrt(sumSquares / values.Count);
        }

        private class Edge
        {
            public PointCloudPoint P1 { get; set; }
            public PointCloudPoint P2 { get; set; }
            public double Weight { get; set; }
        }

        private class UnionFind
        {
            private Dictionary<PointCloudPoint, PointCloudPoint> parent;
            private Dictionary<PointCloudPoint, int> rank;

            public UnionFind(List<PointCloudPoint> points)
            {
                parent = new Dictionary<PointCloudPoint, PointCloudPoint>();
                rank = new Dictionary<PointCloudPoint, int>();

                foreach (var p in points)
                {
                    parent[p] = p;
                    rank[p] = 0;
                }
            }

            public PointCloudPoint Find(PointCloudPoint p)
            {
                if (parent[p] != p)
                    parent[p] = Find(parent[p]);
                return parent[p];
            }

            public void Union(PointCloudPoint p1, PointCloudPoint p2)
            {
                var root1 = Find(p1);
                var root2 = Find(p2);

                if (root1 == root2) return;

                if (rank[root1] < rank[root2])
                    parent[root1] = root2;
                else if (rank[root1] > rank[root2])
                    parent[root2] = root1;
                else
                {
                    parent[root2] = root1;
                    rank[root1]++;
                }
            }
        }
    }

    public class PersonCluster
    {
        public List<PointCloudPoint> Points { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double CenterZ { get; set; }
        public double Height { get; set; }
        public double AvgSNR { get; set; }
        public double MedianDoppler { get; set; }
        public int PointCount { get; set; }
        public double Confidence { get; set; } // 0-100 score
    }
}