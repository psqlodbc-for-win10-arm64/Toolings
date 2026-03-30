using CommandLine;
using LibAmong3.Helpers.PE32;
using LibAmong3.Helpers.PE32.Deeper;
using System.Buffers.Binary;

namespace InspectTLSCallback
{
    internal class Program
    {
        [Verb("inspect")]
        private class InspectOpt
        {
            [Value(0, Required = true, MetaName = "PEInput")]
            public string PEInput { get; set; } = null!;
        }

        [Verb("dummy")]
        private class DummyOpt
        {

        }

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<InspectOpt, DummyOpt>(args)
                .MapResult<InspectOpt, DummyOpt, int>(
                    DoInspect,
                    DoDummy,
                    errs => 1
                );
        }

        private static int DoDummy(DummyOpt opt)
        {
            throw new NotImplementedException();
        }

        private static int DoInspect(InspectOpt opt)
        {
            var pe = File.ReadAllBytes(opt.PEInput).AsMemory();

            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 0)
                {
                    Console.WriteLine("# Before apply Dvrt");
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("# After apply Dvrt");
                    new ApplyDvrtHelper().ApplyDvrt(pe);
                }

                var header = new ParseHeader().Parse(pe);
                Console.WriteLine();
                Console.WriteLine($"  {header.Machine,16:X4} machine");
                var tlsDirectoryEntry = header.GetImageDirectoryOrEmpty(9);
                if (tlsDirectoryEntry.Size != 0)
                {
                    var provider = new VAReadOnlySpanProvider(
                        pe,
                        header.Sections
                    );
                    var tlsHeader = new ParseTLSDirectory().Parse(
                        provider.Provide,
                        tlsDirectoryEntry.VirtualAddress,
                        header.IsPE32Plus
                    )
                        .Header1;

                    Console.WriteLine();
                    Console.WriteLine($"  {tlsHeader.StartOfRawData:X16} Start of raw data");
                    Console.WriteLine($"  {tlsHeader.EndOfRawData:X16} End of raw data");
                    Console.WriteLine($"  {tlsHeader.AddressOfIndex:X16} Address of index");
                    Console.WriteLine($"  {tlsHeader.AddressOfCallback:X16} Address of callbacks");
                    Console.WriteLine($"  {tlsHeader.SizeOfZeroFill,16:X} Size of zero fill");
                    Console.WriteLine($"  {tlsHeader.Characteristics,16:X8} Characteristics");

                    if (tlsHeader.AddressOfCallback != 0)
                    {
                        var tlsCallbacks = new List<ulong>();
                        for (int x = 0; ; x++)
                        {
                            var one = header.IsPE32Plus
                                ? BinaryPrimitives.ReadUInt64LittleEndian(
                                    provider.Provide(Convert.ToInt32(tlsHeader.AddressOfCallback - header.ImageBase + (uint)(8 * x)), 8)
                                )
                                : BinaryPrimitives.ReadUInt32LittleEndian(
                                    provider.Provide(Convert.ToInt32(tlsHeader.AddressOfCallback - header.ImageBase + (uint)(4 * x)), 4)
                                );
                            tlsCallbacks.Add(one);
                            if (one == 0)
                            {
                                break;
                            }
                        }

                        Console.WriteLine();
                        Console.WriteLine("    TLS Callbacks");
                        Console.WriteLine();
                        Console.WriteLine("    Address");
                        Console.WriteLine("    ----------------");

                        foreach (var one in tlsCallbacks)
                        {
                            Console.WriteLine($"    {one:X16}");
                        }
                    }
                }
            }

            return 0;
        }
    }
}
