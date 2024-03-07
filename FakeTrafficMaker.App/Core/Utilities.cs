using System.Buffers;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Security.Cryptography;

namespace FakeTrafficMaker.App.Core
{
    public static class Utilities
    {
        private static readonly RandomNumberGenerator RandomNumberGeneratorInstance = RandomNumberGenerator.Create();
        private static readonly Ping Pinger = new Ping();

        public static bool IsNetworkAvailable()
        {
            // check if network is available and we have connection
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                Debug.Fail("Failed to GetIsNetworkAvailable or no network interface is available!");
                return false;
            }
            return true;
        }
        public static async Task<bool> IsInternetAvailable()
        {
            // check if internet is available and we able to ping google and 4.2.2.4
            if ((await Pinger.SendPingAsync("4.2.2.4", 1000)).Status != IPStatus.Success)
            {
                Debug.Fail("Failed to ping 4.2.2.4 or internet is not available!");
                return false;
            }
            return true;
        }
        public static byte[] GenerateRandomDataBuffer(int minDataSize = 1_000, int multiplier = 10)
        {
            // Generate random data
            byte[] buffer = ArrayPool<byte>.Shared.Rent(minDataSize * multiplier);
            RandomNumberGeneratorInstance.GetBytes(new Span<byte>(buffer));
            return buffer;
        }

        public static void ReturnRandomDataBuffer(in byte[] buffer)
        {
            if (buffer != null)
                ArrayPool<byte>.Shared.Return(buffer);
        }

        public static unsafe byte[] GenerateRandomDataBufferUnsafe(int minDataSize = 1_000, int multiplier = 10)
        {
            // Generate random data with unsafe code memory efficency
            byte[] result = new byte[minDataSize * multiplier];
            //var result = ByteArrayPool.Rent(minDataSize * multiplier);
            fixed (byte* pResult = result)
            {
                byte* current = pResult;

                for (int i = 0; i < multiplier; i++)
                {
                    RandomNumberGeneratorInstance.GetBytes(new Span<byte>(current, minDataSize));
                    current += minDataSize;
                }
            }
            return result;
        }

        public static Settings ParseArguments(this Settings settings, string[] args)
        {
            for (int i = 0; i < args.Length; i += 2)
            {
                if (args[i] == "--concurrent")
                {
                    settings.Concurrency = int.Parse(args[i + 1]);
                }
                else if (args[i] == "--minDataSize")
                {
                    settings.MinDataSize = int.Parse(args[i + 1]);
                }
                else if (args[i] == "--multiplier")
                {
                    settings.Multiplier = int.Parse(args[i + 1]);
                }
                else if (args[i] == "--delay")
                {
                    settings.DelayMinutes = int.Parse(args[i + 1]);
                }
                else if (args[i] == "--verbose")
                {

                }
                else if (args[i] == "--ativationTimes")
                {
                    settings.ActivationTimes = int.Parse(args[i + 1]);
                }
                else
                //else if (args[i] == "--help")
                {
                    Console.WriteLine($"Invalid argument: {args[i]}");
                    PrintCommandLineHelp();
                }
            }

            return settings;
        }

        private static void PrintCommandLineHelp()
        {
            Console.WriteLine($"Usage Help: ");
            Console.WriteLine("FakeTrafficMaker.exe|dll --concurrent <max concurrent tasks> --minDataSize <min upload data size> --multiplier <data size multiplier make random sizes> --delay <delay start> --ativationTimes <ativationTimes 0:second | 1:minutes | 3:hour | 4: day>");
            Console.WriteLine("Example: FakeTrafficMaker.exe|dll --concurrent 2 --minDataSize 100 --multiplier 2 --delay 10 --ativationTimes 0");
        }
    }
}
