using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BarcodeVerificationSystem.Controller;
using BarcodeVerificationSystem.Modules.ReliableDataSender.Core;
using BarcodeVerificationSystem.Modules.ReliableDataSender.Factories;
using BarcodeVerificationSystem.Modules.ReliableDataSender.Services.Woka;
using BarcodeVerificationSystem.View.WokaUI;

namespace WokaIntegrationTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Woka Integration Test ===");
            Console.WriteLine("Test 40 SP/thung voi 160 ma QR");

            // Setup settings
            Shared.Settings.ApiUrl = "http://localhost:5555";
            Shared.Settings.WokaToken = "test-token";
            Shared.Settings.LineIndex = 1;
            Shared.Settings.RLinkName = "Line001";

            string testDbPath = @"D:\R-Link-Branch-Nhan\R_Link\TestData\Woka\TestData_Woka_160codes.csv";
            if (!File.Exists(testDbPath))
            {
                Console.WriteLine($"ERROR: Test database not found: {testDbPath}");
                return;
            }

            string jobFileName = $"TEST_{DateTime.Now:yyMMdd_HHmmss}_LINE_01";
            var job = new BarcodeVerificationSystem.Model.JobModel
            {
                FileName = jobFileName,
                CompareType = BarcodeVerificationSystem.Model.CompareType.Database,
                DirectoryDatabase = testDbPath,
                NumberOfCodesInPallet = 40,
                JobStatus = BarcodeVerificationSystem.Model.JobStatus.Unfinished,
                CartonList = new System.Collections.Generic.List<BarcodeVerificationSystem.Model.Woka.CartonModel>()
            };
            Shared.CurrentJob = job;

            // Initialize AllValues
            Shared.WokaAllValueProcess = new WokaAllValueProcess(job);
            Console.WriteLine($"AllValues file: {Shared.WokaAllValueProcess.getFilePath()}");

            // Create cargo processors
            string storagePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "RLink", "SentDataPallet", $"{jobFileName}.csv");
            string senderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "RLink", "SentDataCargo", $"{jobFileName}.csv");

            Directory.CreateDirectory(Path.GetDirectoryName(storagePath));
            Directory.CreateDirectory(Path.GetDirectoryName(senderPath));

            var storageProcessor = ReliableProcessorFactory.CreateWokaCargoStorageProcessor(storagePath, "", "");
            var senderProcessor = ReliableProcessorFactory.CreateWokaCargoSenderProcessor(senderPath, "", "");

            storageProcessor.Start();
            senderProcessor.Start();

            // Read test QR codes
            var qrCodes = File.ReadAllLines(testDbPath, System.Text.Encoding.UTF8)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Take(160)
                .ToList();

            Console.WriteLine($"Loaded {qrCodes.Count} QR codes");

            // Enqueue all codes as Valid
            // Cau truc codeArray: 0=Index, 1=ResultData(QR), 2=Result, 3=ProcessingTime, 4=DateTime
            int index = 1;
            foreach (var qr in qrCodes)
            {
                string[] codeArray = new string[5];
                codeArray[0] = index.ToString();
                codeArray[1] = qr;
                codeArray[2] = "Valid";
                codeArray[4] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                storageProcessor.Enqueue(index, codeArray);
                senderProcessor.Enqueue(index, codeArray);
                index++;
            }

            Console.WriteLine("Enqueued all codes. Waiting for grouping and sending...");

            // Wait for processing
            int maxWaitSeconds = 120;
            for (int i = 0; i < maxWaitSeconds; i++)
            {
                Thread.Sleep(1000);

                var mappedCount = Shared.WokaAllValueProcess.GetAllValuePayload()?.qr_list?.Count(q => q.status == "mapped") ?? 0;
                var sentCartons = job.CartonList.Count(c => c.IsCodeCartonSent);
                var totalCartons = job.CartonList.Count;

                Console.WriteLine($"[{i + 1}s] mapped={mappedCount}/160, cartons={totalCartons}, sent={sentCartons}");

                if (mappedCount == 160 && sentCartons == 4)
                {
                    Console.WriteLine("SUCCESS: All cartons created and sent!");
                    break;
                }
            }

            // Verify results
            bool success = true;
            var allValues = Shared.WokaAllValueProcess.GetAllValuePayload();
            var mappedCodes = allValues.qr_list.Where(q => q.status == "mapped").ToList();

            Console.WriteLine($"\n=== Verification ===");
            Console.WriteLine($"Total mapped codes: {mappedCodes.Count}");
            Console.WriteLine($"Total cartons: {job.CartonList.Count}");

            if (mappedCodes.Count != 160)
            {
                Console.WriteLine($"FAIL: Expected 160 mapped codes, got {mappedCodes.Count}");
                success = false;
            }

            if (job.CartonList.Count != 4)
            {
                Console.WriteLine($"FAIL: Expected 4 cartons, got {job.CartonList.Count}");
                success = false;
            }

            var cartonGroups = mappedCodes.GroupBy(q => q.qr_pallet).ToList();
            foreach (var group in cartonGroups)
            {
                Console.WriteLine($"Carton {group.Key}: {group.Count()} codes");
                if (group.Count() != 40)
                {
                    Console.WriteLine($"FAIL: Carton {group.Key} has {group.Count()} codes instead of 40");
                    success = false;
                }
            }

            var sentCartonsList = job.CartonList.Where(c => c.IsCodeCartonSent).ToList();
            Console.WriteLine($"Cartons sent to API: {sentCartonsList.Count}/4");
            if (sentCartonsList.Count != 4)
            {
                Console.WriteLine($"FAIL: Expected 4 sent cartons, got {sentCartonsList.Count}");
                success = false;
            }

            storageProcessor.Stop();
            senderProcessor.Stop();

            Console.WriteLine($"\n=== RESULT: {(success ? "PASSED" : "FAILED")} ===");
            Environment.Exit(success ? 0 : 1);
        }
    }
}
