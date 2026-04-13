using NUnit.Framework;
using UnityEngine;

namespace BCIEssentials.Tests.SerialTriggers
{
    using LSLFramework;
    using BCIEssentials.SerialTriggers;

    using P300Encoding = BCIEssentials.SerialTriggers.
        OpinionatedSerialTriggerMarkerWriter.P300Encoding;

    internal class OpinionatedSerialTriggerMarkerWriterTests
    {
        private OpinionatedSerialTriggerMarkerWriter _writer;

        [SetUp]
        public void SetUp()
        {
            GameObject hostObject = new("TestWriter");
            _writer = hostObject.AddComponent<OpinionatedSerialTriggerMarkerWriter>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_writer != null) Object.DestroyImmediate(_writer.gameObject);
        }


        [Test]
        [TestCase(0, 1)]
        [TestCase(1, 2)]
        public void ResolveTriggerCode_WhenMITraining_ThenReturnsTargetIndexPlusOne
        (
            int trainingTarget, int expectedByte
        )
        {
            var marker = new MIEventMarker(stateCount: 2, trainingTargetIndex: trainingTarget, epochLength: 2.0f);
            Assert.AreEqual(expectedByte, _writer.ResolveTriggerCode(marker));
        }

        [Test]
        public void ResolveTriggerCode_WhenMIClassification_ThenReturnsZero()
        {
            var marker = new MIEventMarker(stateCount: 2, trainingTargetIndex: -1, epochLength: 2.0f);
            Assert.AreEqual(0, _writer.ResolveTriggerCode(marker));
        }

        [Test]
        public void ResolveTriggerCode_WhenSSVEPTraining_ThenReturnsTargetIndexPlusOne()
        {
            var marker = new SSVEPEventMarker(
                trainingTargetIndex: 0, epochLength: 4.0f,
                frequencies: new float[] { 8f, 10f, 12f }
            );
            Assert.AreEqual(1, _writer.ResolveTriggerCode(marker));
        }

        [Test]
        public void ResolveTriggerCode_WhenSSVEPClassification_ThenReturnsZero()
        {
            var marker = new SSVEPEventMarker(
                trainingTargetIndex: -1, epochLength: 4.0f,
                frequencies: new float[] { 8f, 10f }
            );
            Assert.AreEqual(0, _writer.ResolveTriggerCode(marker));
        }


        [Test]
        [TestCase(0, 1)]
        [TestCase(2, 3)]
        [TestCase(5, 6)]
        public void StimulusIndexEncoding_WhenSingleFlashP300_ThenReturnsStimulusIndexPlusOne
        (
            int stimulusIndex, int expectedByte
        )
        {
            _writer.P300ResolutionMode = P300Encoding.StimulusIndex;
            var marker = new SingleFlashP300EventMarker(
                presenterCount: 6, trainingTargetIndex: 0, stimulusIndex: stimulusIndex
            );
            Assert.AreEqual(expectedByte, _writer.ResolveTriggerCode(marker));
        }

        [Test]
        public void StimulusIndexEncoding_WhenMultiFlashP300_ThenReturnsIndexFlags()
        {
            _writer.P300ResolutionMode = P300Encoding.StimulusIndex;
            var marker = new MultiFlashP300EventMarker(
                presenterCount: 6, trainingTargetIndex: 0,
                stimulusIndices: new int[] { 3, 5 }
            );
            Assert.AreEqual(0b00101000, _writer.ResolveTriggerCode(marker));
        }

        [Test]
        public void StimulusIndexEncoding_WhenMultiFlashP300EmptyIndices_ThenReturnsZero()
        {
            _writer.P300ResolutionMode = P300Encoding.StimulusIndex;
            var marker = new MultiFlashP300EventMarker(
                presenterCount: 6, trainingTargetIndex: 0,
                stimulusIndices: new int[0]
            );
            Assert.AreEqual(0, _writer.ResolveTriggerCode(marker));
        }


        [Test]
        public void TargetMatchEncoding_WhenSingleFlashTargetTarget_ThenReturnsTargetByte()
        {
            _writer.P300ResolutionMode = P300Encoding.MatchesTarget;
            _writer.TargetByte = 1;
            var marker = new SingleFlashP300EventMarker(
                presenterCount: 6, trainingTargetIndex: 0, stimulusIndex: 0
            );
            Assert.AreEqual(1, _writer.ResolveTriggerCode(marker));
        }

        [Test]
        public void TargetMatchEncoding_WhenSingleFlashNonTarget_ThenReturnsNonTargetByte()
        {
            _writer.P300ResolutionMode = P300Encoding.MatchesTarget;
            _writer.NonTargetByte = 2;
            var marker = new SingleFlashP300EventMarker(
                presenterCount: 6, trainingTargetIndex: 0, stimulusIndex: 1
            );
            Assert.AreEqual(2, _writer.ResolveTriggerCode(marker));
        }

        [Test]
        public void TargetMatchEncoding_WhenMultiFlashContainsTarget_ThenReturnsTargetByte()
        {
            _writer.P300ResolutionMode = P300Encoding.MatchesTarget;
            _writer.TargetByte = 1;
            var marker = new MultiFlashP300EventMarker(
                presenterCount: 6, trainingTargetIndex: 2,
                stimulusIndices: new int[] { 1, 2, 3 }
            );
            Assert.AreEqual(1, _writer.ResolveTriggerCode(marker));
        }
    }
}