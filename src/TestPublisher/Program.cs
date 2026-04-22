using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Opc.Ua;
using Opc.Ua.PubSub;
using Opc.Ua.PubSub.Configuration;

namespace TestPublisher
{
    // Minimal OPC UA PubSub publisher for the integration test suite.
    //
    // Configuration is read from environment variables with the same
    // OPCUA_* prefix pattern as the TestServer services. Every knob is
    // env-driven so one image + different compose env blocks produce
    // different publisher behaviours (unsecured, secured, different
    // publisher ids, ...).
    //
    // Environment variables (all optional, defaults noted):
    //   OPCUA_URL                  opc.udp://HOST:PORT (default: opc.udp://239.0.0.1:4850)
    //   OPCUA_NETWORK_INTERFACE    NIC name (default: "" = all available NICs).
    //                              UA-.NETStandard's PubSub library intentionally
    //                              skips loopback interfaces, so setting this to
    //                              "lo" produces a publisher with zero UdpClients
    //                              — do not pass "lo" as a value.
    //   OPCUA_PUBLISHER_ID         UInt16 (default: 100)
    //   OPCUA_WRITER_GROUP_ID      UInt16 (default: 1)
    //   OPCUA_DATASET_WRITER_ID    UInt16 (default: 1)
    //   OPCUA_DATASET_NAME         string (default: Simple)
    //   OPCUA_PUBLISH_INTERVAL_MS  double (default: 500)
    //   OPCUA_TICK_INTERVAL_MS     double (default: 250)
    //   OPCUA_LOG_LEVEL            Debug|Information|Warning|Error (default: Information)
    //
    // Shape of each NetworkMessage:
    //   PublisherId
    //   WriterGroupId
    //   DataSetWriterId
    //   DataSet with 3 Variant-encoded fields:
    //     counter   UInt32   — monotonically increasing from 0
    //     timestamp DateTime — UTC now at publish time
    //     value     Double    — sine wave between -1.0 and 1.0
    public static class Program
    {
        public static void Main(string[] args)
        {
            var options = PublisherOptions.FromEnvironment();

            Console.WriteLine("=== OPC UA PubSub Test Publisher ===");
            Console.WriteLine($"URL                : {options.PublisherUrl}");
            Console.WriteLine($"Network Interface  : {(options.NetworkInterface == string.Empty ? "(all)" : options.NetworkInterface)}");
            Console.WriteLine($"PublisherId        : {options.PublisherId}");
            Console.WriteLine($"WriterGroupId      : {options.WriterGroupId}");
            Console.WriteLine($"DataSetWriterId    : {options.DataSetWriterId}");
            Console.WriteLine($"DataSetName        : {options.DataSetName}");
            Console.WriteLine($"PublishInterval    : {options.PublishIntervalMs} ms");
            Console.WriteLine($"TickInterval       : {options.TickIntervalMs} ms");

            try
            {
                var telemetry = new ConsoleTelemetry(options.LogLevel);
                var config = BuildConfiguration(options);

                using (var app = UaPubSubApplication.Create(config, telemetry))
                {
                    var simulator = new ValueSimulator(app, options);
                    simulator.Start();

                    app.Start();

                    Console.WriteLine("Publisher started. Press Ctrl-C to exit.");

                    var quit = new ManualResetEvent(false);
                    Console.CancelKeyPress += (_, e) =>
                    {
                        e.Cancel = true;
                        quit.Set();
                    };

                    quit.WaitOne();

                    simulator.Stop();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Publisher failed: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }

        private static PubSubConfigurationDataType BuildConfiguration(PublisherOptions options)
        {
            var connection = new PubSubConnectionDataType
            {
                Name = "TestPubSubConnection",
                Enabled = true,
                PublisherId = options.PublisherId,
                TransportProfileUri = Profiles.PubSubUdpUadpTransport,
                Address = new ExtensionObject(new NetworkAddressUrlDataType
                {
                    NetworkInterface = options.NetworkInterface,
                    Url = options.PublisherUrl,
                }),
                TransportSettings = new ExtensionObject(new DatagramConnectionTransportDataType()),
            };

            var writerGroup = new WriterGroupDataType
            {
                Name = "WriterGroup1",
                Enabled = true,
                WriterGroupId = options.WriterGroupId,
                PublishingInterval = options.PublishIntervalMs,
                KeepAliveTime = 5000,
                MaxNetworkMessageSize = 1500,
                HeaderLayoutUri = "UADP-Cyclic-Fixed",
                MessageSettings = new ExtensionObject(new UadpWriterGroupMessageDataType
                {
                    DataSetOrdering = DataSetOrderingType.AscendingWriterId,
                    GroupVersion = 0,
                    NetworkMessageContentMask = (uint)(
                        UadpNetworkMessageContentMask.PublisherId |
                        UadpNetworkMessageContentMask.GroupHeader |
                        UadpNetworkMessageContentMask.PayloadHeader |
                        UadpNetworkMessageContentMask.WriterGroupId |
                        UadpNetworkMessageContentMask.SequenceNumber),
                }),
                TransportSettings = new ExtensionObject(new DatagramWriterGroupTransportDataType()),
            };

            var dataSetWriter = new DataSetWriterDataType
            {
                Name = "Writer1",
                DataSetWriterId = options.DataSetWriterId,
                Enabled = true,
                DataSetFieldContentMask = (uint)DataSetFieldContentMask.None,
                DataSetName = options.DataSetName,
                KeyFrameCount = 1,
                MessageSettings = new ExtensionObject(new UadpDataSetWriterMessageDataType
                {
                    NetworkMessageNumber = 1,
                    DataSetMessageContentMask = (uint)UadpDataSetMessageContentMask.SequenceNumber,
                }),
            };

            writerGroup.DataSetWriters.Add(dataSetWriter);
            connection.WriterGroups.Add(writerGroup);

            var publishedDataSet = BuildPublishedDataSet(options);

            var config = new PubSubConfigurationDataType();
            config.Connections.Add(connection);
            config.PublishedDataSets.Add(publishedDataSet);

            return config;
        }

        private static PublishedDataSetDataType BuildPublishedDataSet(PublisherOptions options)
        {
            var meta = new DataSetMetaDataType
            {
                DataSetClassId = Uuid.Empty,
                Name = options.DataSetName,
                ConfigurationVersion = new ConfigurationVersionDataType
                {
                    MinorVersion = 0,
                    MajorVersion = 1,
                },
            };

            meta.Fields.Add(new FieldMetaData
            {
                Name = "counter",
                DataSetFieldId = new Uuid(Guid.NewGuid()),
                BuiltInType = (byte)BuiltInType.UInt32,
                DataType = DataTypeIds.UInt32,
                ValueRank = ValueRanks.Scalar,
            });
            meta.Fields.Add(new FieldMetaData
            {
                Name = "timestamp",
                DataSetFieldId = new Uuid(Guid.NewGuid()),
                BuiltInType = (byte)BuiltInType.DateTime,
                DataType = DataTypeIds.DateTime,
                ValueRank = ValueRanks.Scalar,
            });
            meta.Fields.Add(new FieldMetaData
            {
                Name = "value",
                DataSetFieldId = new Uuid(Guid.NewGuid()),
                BuiltInType = (byte)BuiltInType.Double,
                DataType = DataTypeIds.Double,
                ValueRank = ValueRanks.Scalar,
            });

            var source = new PublishedDataItemsDataType();
            source.PublishedData.Add(new PublishedVariableDataType
            {
                PublishedVariable = new NodeId("Counter", 1),
                AttributeId = Attributes.Value,
            });
            source.PublishedData.Add(new PublishedVariableDataType
            {
                PublishedVariable = new NodeId("Timestamp", 1),
                AttributeId = Attributes.Value,
            });
            source.PublishedData.Add(new PublishedVariableDataType
            {
                PublishedVariable = new NodeId("Value", 1),
                AttributeId = Attributes.Value,
            });

            return new PublishedDataSetDataType
            {
                Name = options.DataSetName,
                DataSetMetaData = meta,
                DataSetSource = new ExtensionObject(source),
            };
        }
    }

    // Publisher runtime options, sourced from environment variables with sensible defaults.
    internal sealed class PublisherOptions
    {
        public required string PublisherUrl { get; init; }
        public required string NetworkInterface { get; init; }
        public required ushort PublisherId { get; init; }
        public required ushort WriterGroupId { get; init; }
        public required ushort DataSetWriterId { get; init; }
        public required string DataSetName { get; init; }
        public required double PublishIntervalMs { get; init; }
        public required double TickIntervalMs { get; init; }
        public required LogLevel LogLevel { get; init; }

        public static PublisherOptions FromEnvironment()
        {
            return new PublisherOptions
            {
                PublisherUrl = Env("OPCUA_URL", "opc.udp://239.0.0.1:4850"),
                NetworkInterface = Env("OPCUA_NETWORK_INTERFACE", string.Empty),
                PublisherId = EnvUShort("OPCUA_PUBLISHER_ID", 100),
                WriterGroupId = EnvUShort("OPCUA_WRITER_GROUP_ID", 1),
                DataSetWriterId = EnvUShort("OPCUA_DATASET_WRITER_ID", 1),
                DataSetName = Env("OPCUA_DATASET_NAME", "Simple"),
                PublishIntervalMs = EnvDouble("OPCUA_PUBLISH_INTERVAL_MS", 500),
                TickIntervalMs = EnvDouble("OPCUA_TICK_INTERVAL_MS", 250),
                LogLevel = EnvLogLevel("OPCUA_LOG_LEVEL", LogLevel.Information),
            };
        }

        private static string Env(string key, string fallback)
        {
            var raw = Environment.GetEnvironmentVariable(key);

            return string.IsNullOrEmpty(raw) ? fallback : raw;
        }

        private static ushort EnvUShort(string key, ushort fallback)
        {
            var raw = Environment.GetEnvironmentVariable(key);

            return ushort.TryParse(raw, out var value) ? value : fallback;
        }

        private static double EnvDouble(string key, double fallback)
        {
            var raw = Environment.GetEnvironmentVariable(key);

            return double.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : fallback;
        }

        private static LogLevel EnvLogLevel(string key, LogLevel fallback)
        {
            var raw = Environment.GetEnvironmentVariable(key);

            return Enum.TryParse<LogLevel>(raw, ignoreCase: true, out var value) ? value : fallback;
        }
    }

    // Feeds deterministic values into the publisher's DataStore on a timer.
    internal sealed class ValueSimulator
    {
        private readonly UaPubSubApplication _application;
        private readonly System.Timers.Timer _timer;
        private uint _counter;

        public ValueSimulator(UaPubSubApplication application, PublisherOptions options)
        {
            _application = application;
            _timer = new System.Timers.Timer(options.TickIntervalMs);
            _timer.Elapsed += (_, _) => Tick();
            _timer.AutoReset = true;
        }

        public void Start()
        {
            Tick();
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            _timer.Dispose();
        }

        private void Tick()
        {
            _counter++;
            var now = DateTime.UtcNow;
            var value = Math.Sin(_counter * Math.PI / 20.0);

            _application.DataStore.WritePublishedDataItem(
                new NodeId("Counter", 1), Attributes.Value, new DataValue(new Variant(_counter)));
            _application.DataStore.WritePublishedDataItem(
                new NodeId("Timestamp", 1), Attributes.Value, new DataValue(new Variant(now)));
            _application.DataStore.WritePublishedDataItem(
                new NodeId("Value", 1), Attributes.Value, new DataValue(new Variant(value)));
        }
    }

    // Telemetry context that routes UA-.NETStandard internal diagnostics to the console.
    internal sealed class ConsoleTelemetry : TelemetryContextBase
    {
        public ConsoleTelemetry(LogLevel minimumLevel) : base(Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(minimumLevel);
        }))
        {
        }
    }
}
