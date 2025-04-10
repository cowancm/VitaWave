using ModuleControl.Parsing;

namespace ModuleControlTests.Parsing
{
    public class FrameParserTests
    {
        private readonly string _sampleDataPath;
        int[] MAGIC_WORD = { 2, 1, 4, 3, 6, 5, 8, 7 };
        string _sampleDataFolderName = "SampleData";

        public FrameParserTests()
        {
            _sampleDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _sampleDataFolderName);
        }

        [Fact] //currently this fails. For some reason half of the sample data is filled with 0s
        public void AllSampleDataFiles_ShouldParseSuccessfully()
        {
            List<(Event?, byte[], string)> values = new List<(Event?, byte[], string)>(); //Way to keep track of our events/bytes/fileName in debug

            // arrange
            var sampleFiles = Directory.GetFiles(_sampleDataPath, "output*.bin");
            sampleFiles.Should().NotBeEmpty();

            foreach (var filePath in sampleFiles)
            {
                // act
                var fileName = Path.GetFileName(filePath);

                var fileBytes = File.ReadAllBytes(filePath).AsSpan();

                // the first 8 bytes should be the magic word
                IsMagicWordDetected(fileBytes.Slice(0, 8)).Should().BeTrue();

                // the next 32 should be the rest of the header
                var headerBytes = fileBytes.Slice(8, 32);
                var header = FrameParser.CreateFrameHeader(headerBytes);
                header.NumTLVs.Should().BeGreaterThan(0);


                //we want to now read after the magicword/header, packet length includes the header as well
                var headerOffset = 40;
                var tlvBytes = fileBytes.Slice(headerOffset, (int)header.TotalPacketLength - headerOffset);


                var evt = FrameParser.CreateEvent(tlvBytes, header);
                var bytes = new byte[header.TotalPacketLength];
                fileBytes.CopyTo(bytes);
                values.Add((evt, bytes, fileName));
            }

            //this is me debugging here
            var allThatIsNull = values
                                .Where(x => x.Item1 == null)
                                .OrderBy(x => x.Item3)
                                .ToList();

            var allThatIsNotNull = values.Where(x => x.Item1 != null)
                                         .OrderBy(x => x.Item3)
                                         .ToList();

            (allThatIsNotNull.Count + allThatIsNull.Count).Should().Be(sampleFiles.Count());

            allThatIsNull.Count.Should().Be(0);
        }

        private bool IsMagicWordDetected(Span<byte> possibleMagicWord)
        {
            int i = 0;
            foreach (var value in possibleMagicWord)
            {
                if (value != MAGIC_WORD[i])
                    return false;
                i++;
            }
            return true;
        }
    }
}

