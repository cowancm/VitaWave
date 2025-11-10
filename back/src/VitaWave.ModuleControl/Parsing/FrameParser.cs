using Serilog;
using System.Runtime.InteropServices;
using VitaWave.Common;
using VitaWave.Common.TLVs;
using VitaWave.ModuleControl.Parsing.TLVs;
using static VitaWave.ModuleControl.Parsing.TLVs.TLV_Constants;

namespace VitaWave.ModuleControl.Parsing
{
    public static class FrameParser
    {
        public static FrameHeader CreateFrameHeader(Span<byte> data)
        {
            var result = new FrameHeader()
            {
                Version = MemoryMarshal.Read<uint>(data.Slice(0, 4)),
                TotalPacketLength = MemoryMarshal.Read<uint>(data.Slice(4, 4)),
                Platform = MemoryMarshal.Read<uint>(data.Slice(8, 4)),
                FrameNumber = MemoryMarshal.Read<uint>(data.Slice(12, 4)),
                Time = MemoryMarshal.Read<uint>(data.Slice(16, 4)),
                NumDetectedObj = MemoryMarshal.Read<uint>(data.Slice(20, 4)),
                NumTLVs = MemoryMarshal.Read<uint>(data.Slice(24, 4)),
                SubframeNumber = MemoryMarshal.Read<uint>(data.Slice(28, 4))
            };

            return result;
        }

        public static (EventPacket?, List<uint>?) CreateEvent(Span<byte> tlvBuffer, FrameHeader frameHeader)
        {
            var resultingEvent = new EventPacket();
            List<uint>? targetIndices = null;
            var indexInTlvBuffer = 0;
            var numTlvsRead = 0;

            try //if the bytes fail to be read correctly, throw it out and turn it null
            {
                List<TargetHeight>? heights = null;

                while (numTlvsRead != frameHeader.NumTLVs)
                {
                    var tlvHeader = TLVHeaderParser.GetHeaderTypeSize(tlvBuffer.Slice(indexInTlvBuffer, TLVHeaderParser.HEADER_LENGTH));
                    indexInTlvBuffer += TLVHeaderParser.HEADER_LENGTH;

                    var headerType = tlvHeader.Item1;
                    var numBytesInThisTlv = tlvHeader.Item2;

                    switch (headerType)
                    {
                        case TLV_TYPE.POINT_CLOUD:
                            resultingEvent.Points = CreatePointCloud(tlvBuffer.Slice(indexInTlvBuffer, numBytesInThisTlv));
                            break;
                        case TLV_TYPE.TARGET_LIST:
                            resultingEvent.Targets = CreateTargets(tlvBuffer.Slice(indexInTlvBuffer, numBytesInThisTlv));
                            break;
                        case TLV_TYPE.TARGET_INDEX:
                            targetIndices = CreateTargetIndices(tlvBuffer.Slice(indexInTlvBuffer, numBytesInThisTlv));
                            break;
                        case TLV_TYPE.TARGET_HEIGHT:
                            heights = CreateTargetHeights(tlvBuffer.Slice(indexInTlvBuffer, numBytesInThisTlv));
                            break;
                        case TLV_TYPE.PRESENCE_INDICATION:
                            resultingEvent.Presence = CreateIsPresent(tlvBuffer.Slice(indexInTlvBuffer, numBytesInThisTlv));
                            break;
                        default:
                            throw new ArgumentException("Bad TLV Header");
                    }
                    indexInTlvBuffer += numBytesInThisTlv;
                    numTlvsRead++;
                }

                if (heights != null)
                {
                    if (heights?.Count == resultingEvent.Targets?.Count)
                        for (int i = 0; i < heights!.Count; i++)
                        {
                            resultingEvent.Targets![i].TargetHeight = heights[i];
                        }
                    else
                        throw new ArgumentException("Target heights count doesn't match target list count");
                }
                
            }
            catch (Exception ex) 
            {
                Log.Error(ex, $"Error parsing TLV data: {ex.Message}");
                resultingEvent = null;
            }

            return (resultingEvent, targetIndices);
        }

        #region PointCloud

        private const int LENGTH_PER_POINT_CLOUD = 8;
        private const int LENGTH_PER_POINT_UNITS = 20;

        private static List<PointCloudPoint> CreatePointCloud(Span<byte> data)
        {
            var numPoints = (data.Length - LENGTH_PER_POINT_UNITS) / LENGTH_PER_POINT_CLOUD; //first couple bytes are the point unit, then the rest are points
            var points = new List<PointCloudPoint>();

            var elevationUnit = MemoryMarshal.Read<float>(data.Slice(0, 4));
            var azmithUnit = MemoryMarshal.Read<float>(data.Slice(4, 4));
            var dopplerUnit = MemoryMarshal.Read<float>(data.Slice(8, 4));
            var rangeUnit = MemoryMarshal.Read<float>(data.Slice(12, 4));
            var snrUnit = MemoryMarshal.Read<float>(data.Slice(16, 4));

            for (int i = 0; i < numPoints; i++)
            {
                var offset = i * LENGTH_PER_POINT_CLOUD + LENGTH_PER_POINT_UNITS;
                var targetedPortion = data.Slice(offset, LENGTH_PER_POINT_CLOUD);

                var point = CreatePoint(targetedPortion, elevationUnit, azmithUnit, dopplerUnit, rangeUnit, snrUnit);
                points.Add(point);
            }

            return points;
        }

        private static PointCloudPoint CreatePoint(Span<byte> data, float elevationUnit, float azimuthUnit, float dopplerUnit, float rangeUnit, float snrUnit)
        {
            var elevation = elevationUnit * (double)(sbyte)data[0];
            var azimuth = azimuthUnit * (double)(sbyte)data[1];
            var range = rangeUnit * (double)MemoryMarshal.Read<Int16>(data.Slice(4, 2));
            var point = new PointCloudPoint()
            {
                X = range * Math.Sin(azimuth) * Math.Cos(elevation),
                Y = range * Math.Cos(azimuth) * Math.Cos(elevation),
                Z = range * Math.Sin(elevation),
                Doppler = dopplerUnit * (double)MemoryMarshal.Read<Int16>(data.Slice(2, 2)),
                SNR = snrUnit * (double)MemoryMarshal.Read<Int16>(data.Slice(6, 2)),
            };

            return point;
        }
        #endregion

        #region Target Object List

        public const int BYTES_PER_TARGET = 112;

        private static List<Target> CreateTargets(Span<byte> data)
        {
            var numTargets = data.Length / BYTES_PER_TARGET;
            var targets = new List<Target>();

            for (int i = 0; i < numTargets; i++)
            {
                var offset = i * BYTES_PER_TARGET;
                var targetedPortion = data.Slice(offset, BYTES_PER_TARGET);

                var target = CreateTarget(targetedPortion);
                targets.Add(target);
            }

            return targets;
        }

        private static Target CreateTarget(Span<byte> data)
        {
            // Get error covariance matrix
            List<float> ec = new();
            for (int i = 0; i < 16; i++)
            {
                ec.Add(MemoryMarshal.Read<float>(data.Slice(40 + i * 4, 4)));
            }

            var target = new Target()
            {
                TID = MemoryMarshal.Read<uint>(data.Slice(0, 4)),
                X = MemoryMarshal.Read<float>(data.Slice(4, 4)),
                Y = MemoryMarshal.Read<float>(data.Slice(8, 4)),
                Z = MemoryMarshal.Read<float>(data.Slice(12, 4)),
                VelX = MemoryMarshal.Read<float>(data.Slice(16, 4)),
                VelY = MemoryMarshal.Read<float>(data.Slice(20, 4)),
                VelZ = MemoryMarshal.Read<float>(data.Slice(24, 4)),
                AccX = MemoryMarshal.Read<float>(data.Slice(28, 4)),
                AccY = MemoryMarshal.Read<float>(data.Slice(32, 4)),
                AccZ = MemoryMarshal.Read<float>(data.Slice(36, 4)),
                Ec = ec,
                G = MemoryMarshal.Read<float>(data.Slice(104, 4)),
                ConfidenceLevel = MemoryMarshal.Read<float>(data.Slice(108, 4))
            };

            return target;
        }
        #endregion

        #region Target Indicies

        private static List<uint> CreateTargetIndices(Span<byte> data)
        {
            var targets = new List<uint>();
            foreach (var target in data)
            {
                targets.Add(target);
            }
            return targets;
        }

        #endregion

        #region Target Heights

        private const int TARGET_HEIGHT_LENGTH = 12;

        private static List<TargetHeight> CreateTargetHeights(Span<byte> data)
        {
            var numTargetHeights = data.Length / TARGET_HEIGHT_LENGTH;
            var targetHeights = new List<TargetHeight>();

            for (int i = 0; i < numTargetHeights; i++)
            {
                var offset = i * TARGET_HEIGHT_LENGTH;
                var targetedPortion = data.Slice(offset, TARGET_HEIGHT_LENGTH);

                var targetHeight = CreateTargetHeight(targetedPortion);
                targetHeights.Add(targetHeight);
            }

            return targetHeights;
        }

        private static TargetHeight CreateTargetHeight(Span<byte> data)
        {
            // Log.Information("Raw data (hex): {HexData}", Convert.ToHexString(data));

            var targetHeight = new TargetHeight()
            {
                TargetID = (uint)BitConverter.ToInt32(data.Slice(0, 4)),
                MaxZ = MemoryMarshal.Read<float>(data.Slice(4, 4)),
                MinZ = MemoryMarshal.Read<float>(data.Slice(8, 4)),
            };

            // Log.Information("Parsed TargetHeight -> ID: {TargetID}, MaxZ: {MaxZ}, MinZ: {MinZ}",
            //    targetHeight.TargetID, targetHeight.MaxZ, targetHeight.MinZ); thank you chatgpt for helping me debug this one

            return targetHeight;
        }

        #endregion

        #region Presence Indication
        private static bool CreateIsPresent(Span<byte> data)
        {
            return MemoryMarshal.Read<int>(data) > 0;
        }

        #endregion
    }
}
