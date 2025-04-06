using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using ModuleControl.Parsing;
using ModuleControl.Parsing.TLVs;
using ModuleControl.Communication;

namespace ModuleControl.Tests
{
    public class FrameParserTests
    {
        private readonly string _sampleDataPath;
        int[] MAGIC_WORD = { 2, 1, 4, 3, 6, 5, 8, 7 };

        public FrameParserTests()
        {
            _sampleDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SampleData");
        }

        [Fact]
        public void AllSampleDataFiles_ShouldParseSuccessfully()
        {
            List<(Event?, byte[])> values = new List<(Event?, byte[])>();
            var count = 0;

            // arrange
            var sampleFiles = Directory.GetFiles(_sampleDataPath, "output*.bin");
            sampleFiles.Should().NotBeEmpty();

            foreach (var filePath in sampleFiles)
            {
                // act
                var fileName = Path.GetFileName(filePath);

                var fileBytes = File.ReadAllBytes(filePath).AsSpan();
                    
                // The first 8 bytes should be the magic word
                IsMagicWordDetected(fileBytes.Slice(0, 8)).Should().BeTrue();

                var headerBytes = fileBytes.Slice(8, 32);
                var header = FrameParser.CreateFrameHeader(headerBytes);

                header.NumTLVs.Should().BeGreaterThan(0);

                var headerOffset =  40;
                var tlvBytes = fileBytes.Slice(headerOffset, (int) header.TotalPacketLength - headerOffset);


                var evt = FrameParser.CreateEvent(tlvBytes, header);
                var bytes = new byte[header.TotalPacketLength];
                fileBytes.CopyTo(bytes);
                values.Add((evt, bytes));

                if (evt is null)
                    count++;
            }

            var allThatIsNull = values.Where(x => x.Item1 == null).ToList();
            var allThatIsNotNull = values.Where(x => x.Item1 != null).ToList();

            count.Should().Be(0);
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

       