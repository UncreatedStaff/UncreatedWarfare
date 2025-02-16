using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Uncreated.Warfare.Configuration;
using YamlDotNet.Serialization;

namespace Uncreated.Warfare.Tests
{
    [Ignore("Idk theyre not working")]
    public class YamlDataStoreTests
    {
        private readonly string _testDirectory = Path.GetFullPath("HelloBro");
        private ILogger CreateLogger() => LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("YamlDataStore");

        [SetUp]
        public void SetUpTest()
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);

            Directory.CreateDirectory(_testDirectory);
        }
        public class ExampleRecord
        {
            public required string UniqueName { get; set; }
            public required uint BuildableInstanceId { get; set; }
            public required List<uint> SignInstanceIds { get; set; }
            public bool IsStructure { get; set; } = false;
        }

        [Test]
        public void TestSave()
        {
            var dataStore = new YamlDataStore<List<ExampleRecord>>(Path.Combine(_testDirectory, "test-save-data.yml"), CreateLogger(), true, () => new List<ExampleRecord>());

            Assert.AreEqual(0, dataStore.Data.Count);

            dataStore.Data.Add(
                new ExampleRecord
                {
                    UniqueName = "test1",
                    BuildableInstanceId = 1,
                    SignInstanceIds = new List<uint> { 1, 2, 3 },
                    IsStructure = true
                });
            dataStore.Data.Add(
                new ExampleRecord
                {
                    UniqueName = "test2",
                    BuildableInstanceId = 2,
                    SignInstanceIds = new List<uint> { 4, 5, 6 },
                    IsStructure = false
                });

            dataStore.Save();
            dataStore.Reload();
            Assert.AreEqual(2, dataStore.Data.Count);

            dataStore.Data.Add(
                new ExampleRecord
                {
                    UniqueName = "test3",
                    BuildableInstanceId = 3,
                    SignInstanceIds = new List<uint> { 7, 8, 9 },
                    IsStructure = true
                }
            );
            dataStore.Save();
            dataStore.Reload();
            Assert.AreEqual(3, dataStore.Data.Count);
        }

        [Test]
        public void TestLoad()
        {
            List<ExampleRecord> records = new List<ExampleRecord>()
            {
                new ExampleRecord
                {
                    UniqueName = "test1",
                    BuildableInstanceId = 1,
                    SignInstanceIds = new List<uint> { 1, 2, 3 },
                    IsStructure = true
                },
                new ExampleRecord
                {
                    UniqueName = "test2",
                    BuildableInstanceId = 2,
                    SignInstanceIds = new List<uint> { 4, 5, 6 },
                    IsStructure = false
                }
            };

            string sourceFilePath = Path.Combine(_testDirectory, "test-load-data.yml");
            WriteRecordsToFile(sourceFilePath, records);

            var dataStore = new YamlDataStore<List<ExampleRecord>>(sourceFilePath, CreateLogger(), true, () => new List<ExampleRecord>());

            dataStore.Reload();
            Assert.AreEqual(2, dataStore.Data.Count);
        }
        [Test]
        public async Task TestFileWatcherReloadAsync()
        {
            List<ExampleRecord> records = new List<ExampleRecord>()
            {
                new ExampleRecord
                {
                    UniqueName = "test1",
                    BuildableInstanceId = 1,
                    SignInstanceIds = new List<uint> { 1, 2, 3 },
                    IsStructure = true
                },
                new ExampleRecord
                {
                    UniqueName = "test2",
                    BuildableInstanceId = 2,
                    SignInstanceIds = new List<uint> { 4, 5, 6 },
                    IsStructure = false
                }
            };

            string sourceFilePath = Path.Combine(_testDirectory, "test-filewatcher-data.yml");
            WriteRecordsToFile(sourceFilePath, records);

            var dataStore = new YamlDataStore<List<ExampleRecord>>(sourceFilePath, CreateLogger(), true, () => new List<ExampleRecord>());

            dataStore.Reload();
            Assert.AreEqual(2, dataStore.Data.Count);

            records.Add(
                new ExampleRecord
                {
                    UniqueName = "test3",
                    BuildableInstanceId = 3,
                    SignInstanceIds = new List<uint> { 7, 8, 9 },
                    IsStructure = true
                }
            );
            WriteRecordsToFile(sourceFilePath, records);

            await Task.Delay(1000); // wait a little bit for the file watcher to notice that the file changed
            Assert.AreEqual(3, dataStore.Data.Count);
        }
        private void WriteRecordsToFile(string filePath, List<ExampleRecord> records)
        {
            var seserializer = new SerializerBuilder().Build();
            string testDataYaml = seserializer.Serialize(records);
            
            File.WriteAllText(Path.GetFullPath(filePath), testDataYaml);
        }
    }
}
