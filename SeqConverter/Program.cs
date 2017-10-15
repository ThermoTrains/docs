using System;
using System.IO;
using SebastianHaeni.ThermoBox.IRCompressor;
using SebastianHaeni.ThermoBox.IRDecompressor;

namespace SeqConverter
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var (input, output, mode) = ParseArguments(args);

            if (IsSeqFile(input) && IsMp4File(output))
            {
                IRSensorDataCompression.Compress(input, output, mode);
            }
            else if (IsMp4File(input) && IsSeqFile(output))
            {
                IRSensorDataDecompression.Decompress(input, output);
            }
            else
            {
                Console.WriteLine("Invalid combination of seq and mp4");
                Environment.Exit(1);
            }
        }

        private static bool IsSeqFile(string input)
        {
            return input != null && Path.GetExtension(input).ToLowerInvariant().Equals(".seq");
        }

        private static bool IsMp4File(string input)
        {
            return input != null && Path.GetExtension(input).ToLowerInvariant().Equals(".mp4");
        }

        private static (string input, string output, IRSensorDataCompression.Mode mode) ParseArguments(string[] args)
        {
            if (args.Length != 2 && args.Length != 3)
            {
                Console.WriteLine(@"Usage:
seqconverter <input> <output> [mode]

Converting .seq to mp4:
seqconverter myseq.seq myseq.mp4

Converting a previously converted mp4 back to .seq:
seqconverter myseq.mp4 myseq.seq

Available modes:
- other: uses the whole image
- train: tries to find horizontal movement and limits compression on this area");
                Environment.Exit(1);
            }

            var input = args[0];
            var output = args[1];
            var mode = IRSensorDataCompression.Mode.Other;

            if (args.Length == 3)
            {
                Enum.TryParse(args[2], out mode);
            }

            return (input, output, mode);
        }
    }
}
