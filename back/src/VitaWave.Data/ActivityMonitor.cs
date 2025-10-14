using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using Newtonsoft.Json;

// Data structures from parser
public class Point
{
    public int TID { get; set; }        
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double Doppler { get; set; }
    public double SNR { get; set; }
}

public class Frame
{
    public List<Point> Points { get; set; }
    public bool Presence { get; set; }
    public double TimeSinceLastMs { get; set; } 
}

// OG Algorithm
public enum Posture
{
    Standing,
    Sitting,
    Walking,
    Laying,
    Falling,
    Unknown
}

public class Person
{
    public Vector3 PreviousPosition { get; set; }
    public Vector3 CurrentPosition { get; set; }
    public double DeltaTime { get; set; }
    public double Velocity { get; private set; }
    public double VerticalChange { get; private set; }
    public double TimeSinceLastFallCheck { get; set; } = 0;
    public bool FallFlag { get; private set; } = false;

    private const double WALKING_VELOCITY = 0.2;
    private const double FALLING_VELOCITY = 1.5;
    private const double FALLING_Z_DROP = 1.0;
    private const double STANDING_Z = 1.4;
    private const double SITTING_Z_MIN = 0.7;
    private const double LAYING_Z = 0.6;

    public double SensorTiltDegrees { get; set; } = 15.0;       // Downward tilt angle in degrees
    public double SensorHeight { get; set; } = 2.0;     // Height of radar module above the floor (meters) (CHANGE THIS LATER)

    // Polynomial bias correction (fit from calibration) (CAN CHANGE THESE LATER)
    // Example: b(r) = a2*r^2 + a1*r + a0
    private const double a2 = 0.002;        
    private const double a1 = -0.01;
    private const double a0 = 0.0;

    public void UpdatePosition(Vector3 newPosition, double deltaTime)       // Updates new position and calculates velocity & time differential
    {
        PreviousPosition = CurrentPosition;
        CurrentPosition = CalibratePosition(newPosition);
        DeltaTime = deltaTime;
        ComputeVelocity();
    }

    private Vector3 CalibratePosition(Vector3 raw)
    {
        // raw: (x, y, z) from radar in radar frame
        // Assume Z is along radar’s local "up" axis, Y forward, X lateral

        // 1. Correct for sensor tilt (rotate around X-axis)
        double tiltRad = SensorTiltDegrees * Math.PI / 180.0;       
        double yCorrected = raw.Y * Math.Cos(tiltRad) + raw.Z * Math.Sin(tiltRad);
        double zCorrected = -raw.Y * Math.Sin(tiltRad) + raw.Z * Math.Cos(tiltRad);
        double zWorld = SensorHeight - zCorrected;           // 2. Translate to world coordinates (Z=0 is floor)

        // 3. Distance-based bias correction
        double range = Math.Sqrt(raw.X * raw.X + raw.Y * raw.Y);        // horizontal distance
        double bias = a2 * range * range + a1 * range + a0;
        zWorld -= bias;       // subtract bias (if far targets appear lower)

        return new Vector3((float)raw.X, (float)yCorrected, (float)zWorld);
    }

    private void ComputeVelocity()              // Calculates velocity with positional differentials
    {
        double dx = CurrentPosition.X - PreviousPosition.X;
        double dy = CurrentPosition.Y - PreviousPosition.Y;
        double dz = CurrentPosition.Z - PreviousPosition.Z;

        VerticalChange = dz;
        Velocity = Math.Sqrt(dx * dx + dy * dy + dz * dz) / DeltaTime;
    }

    public Posture ClassifyPosture()            // Determines positions
    {
        double height = CurrentPosition.Z;      // now calibrated height above floor

        if (VerticalChange < -FALLING_Z_DROP && Velocity > FALLING_VELOCITY)
        {
            FallFlag = true;
            TimeSinceLastFallCheck = 0;
            return Posture.Falling;
        }

        if (FallFlag)
        {
            TimeSinceLastFallCheck += DeltaTime;
            if (TimeSinceLastFallCheck > 0.5)
                FallFlag = false;
            return Posture.Falling;
        }

        if (Velocity > WALKING_VELOCITY)
            return Posture.Walking;

        if (height > STANDING_Z)
            return Posture.Standing;

        if (height > SITTING_Z_MIN && height <= STANDING_Z)
            return Posture.Sitting;

        if (height <= LAYING_Z)
            return Posture.Laying;

        return Posture.Unknown;
    }
}

// Radar Processor for differenciating individual people 
// Good for testing, not sure if we'll still need this in the final product if we have a parser too
public class RadarProcessor
{
    private Dictionary<int, Person> trackedPersons = new(); // key = TID

    public void ProcessRadarFile(string jsonPath)
    {
        string json = File.ReadAllText(jsonPath);
        var frames = JsonConvert.DeserializeObject<List<Frame>>(json);

        Console.WriteLine($"Loaded {frames.Count} frames.\n");

        double previousTimestamp = 0;

        foreach (var frame in frames)
        {
            double deltaTime = frame.TimeSinceLastMs / 1000.0; // convert to seconds
            previousTimestamp += deltaTime;

            if (frame.Points == null || frame.Points.Count == 0)
                continue;

            foreach (var p in frame.Points)
            {
                if (!trackedPersons.ContainsKey(p.TID))
                    trackedPersons[p.TID] = new Person();

                var person = trackedPersons[p.TID];
                Vector3 radarPos = new((float)p.X, (float)p.Y, (float)p.Z);
                person.UpdatePosition(radarPos, deltaTime);
                Posture posture = person.ClassifyPosture();

                Console.WriteLine($"TID {p.TID}: Height={person.CurrentPosition.Z:F2}m, Vel={person.Velocity:F2}m/s, Posture={posture}");
            }

            Console.WriteLine();
        }
    }
}

//  Main for testing
class Program
{
    static void Main()
    {
        string path = @"C:\Users\matir\Downloads\data (1)\data"; // CHANGE TO YOUR FILE PATH
        RadarProcessor processor = new();
        processor.ProcessRadarFile(path);
    }
}
