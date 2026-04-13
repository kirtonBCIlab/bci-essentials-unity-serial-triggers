using System;
using NUnit.Framework;
using UnityEngine;

namespace BCIEssentials.Tests.SerialTriggers
{
    using System.Threading;
    using LSLFramework;
    using BCIEssentials.SerialTriggers;

    internal class SerialTriggerWriterTests
    {
        const int ExpectedTransmissionDelay = 10;
        const int TestPulseWidth = 100;
        private DummySerialMarkerWriter _writer;

        [SetUp]
        public void SetUp()
        {
            GameObject hostObject = new("TestWriter");
            _writer = hostObject.AddComponent<DummySerialMarkerWriter>();
            _writer.PulseWidth = TestPulseWidth;
        }

        [TearDown]
        public void TearDown()
        {
            if (_writer != null) UnityEngine.Object.DestroyImmediate(_writer.gameObject);
        }


        [Test]
        [TestCase(typeof(TrialStartedMarker), 0xf1)]
        [TestCase(typeof(TrialEndsMarker), 0xf0)]
        [TestCase(typeof(TrainingCompleteMarker), 0xf2)]
        [TestCase(typeof(TrainClassifierMarker), 0xf3)]
        [TestCase(typeof(UpdateClassifierMarker), 0xf4)]
        [TestCase(typeof(DoneWithRestingStateCollectionMarker), 0xf5)]
        public void ResolveTriggerCode_WhenStatusMarker_ThenReturnsConfiguredByte
        (
            Type markerType, int expectedByte
        )
        {
            var marker = (IStatusMarker)Activator.CreateInstance(markerType);
            Assert.AreEqual(expectedByte, _writer.ResolveTriggerCode(marker));
        }


        [Test]
        public void FakeMode_WhenConnected_ThenSendMarkerTriggerSendsPulseAndReset()
        {
            _writer.PushMarker(new TrialStartedMarker());
            Thread.Sleep(ExpectedTransmissionDelay);

            Assert.AreEqual(0xf1, _writer.LastByteSent);
            Thread.Sleep(TestPulseWidth);

            Assert.AreEqual(0, _writer.LastByteSent);
            Assert.AreEqual(2, _writer.BytesWritten);
        }

        [Test]
        public void FakeMode_WhenConnected_ThenSendPulseSendsValueThenZero()
        {
            _writer.SendPulse(42);
            Thread.Sleep(ExpectedTransmissionDelay);

            Assert.AreEqual(42, _writer.LastByteSent);
            Thread.Sleep(TestPulseWidth);

            Assert.AreEqual(0, _writer.LastByteSent);
            Assert.AreEqual(2, _writer.BytesWritten);
        }


        private class DummySerialWriter : SerialPortPulseWriter
        {
            public byte LastByteSent;
            public int BytesWritten = 0;

            public void FakeConnect() => SetUp();

            public override void SendByte(byte value)
            {
                LastByteSent = value;
                BytesWritten++;
            }
        }

        private class DummySerialMarkerWriter : SerialTriggerMarkerWriter
        {
            public DummySerialWriter FakeConnection => _writer as DummySerialWriter;
            public byte LastByteSent => FakeConnection.LastByteSent;
            public int BytesWritten => FakeConnection.BytesWritten;

            public override void PushMarker(IMarker marker)
            {
                byte resolvedValue = ResolveTriggerCode(marker);
                SendPulse(resolvedValue);
            }

            public void SendPulse(byte value)
            {
                if (_writer == null)
                {
                    _writer = new DummySerialWriter();
                    FakeConnection.FakeConnect();
                }

                _writer.PulseWidth = PulseWidth;
                _writer.SendPulse(value);
            }

            protected override byte ResolveEventMarkerTriggerCode(EventMarker marker) => UnresolvedByte;
        }
    }
}
