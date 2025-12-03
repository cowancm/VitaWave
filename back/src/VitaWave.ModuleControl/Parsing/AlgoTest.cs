/*
using VitaWave.ModuleControl.Parsing;
using VitaWave.Common.ModuleToAPI.TLVs;

namespace AlgoTest
{
    public static class BehaviorClassifier
    {
        public static string Classify(ParsingEvent e)
        {
            var points = e.Points;
            var targets = e.Targets;
            var heights = e.Heights;

            if (points == null || targets == null || heights == null)
                return "Unknown"; // Or handle error

            foreach (var height in heights)
            {
                if (height.HeightInCm < 70)
                    return "Sitting";
                else if (height.HeightInCm > 140)
                    return "Standing";
            }

            return "No Person Detected";
        }
    }
}
*/
/*
 var classification = AlgoTest.BehaviorClassifier.Classify(e);
                System.Console.WriteLine("Behavior Classification: " + classification); */