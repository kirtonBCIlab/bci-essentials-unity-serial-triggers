using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace BCIEssentials.SerialTriggers
{
    public class SerialPortPulseWriter : SerialPortWriter
    {
        public int PulseWidth;

        protected bool WriterThreadExistsAndIsAlive => _writerThread?.IsAlive == true;
        private bool PulseQueueIsWritable => _pulseQueue != null && !_pulseQueue.IsAddingCompleted;
        private BlockingCollection<byte> _pulseQueue;
        private Thread _writerThread;


        ~SerialPortPulseWriter() => Disconnect();

        public void SendPulse(byte value)
        {
            if (PulseQueueIsWritable) _pulseQueue.Add(value);
            else
            {
                Debug.LogWarning("Pulse queue not available, sending on main thread");
                WritePulse(value, PulseWidth);
            }
        }


        protected override void SetUp() => StartWriterThread();
        protected override void CleanUp() => StopWriterThread();

        private void StartWriterThread()
        {
            if (WriterThreadExistsAndIsAlive) return;

            _pulseQueue = new();
            _writerThread = new Thread(WritePulsesFromQueue)
            {
                Name = "Serial Pulse Writer",
                IsBackground = true
            };
            _writerThread.Start();
        }

        private void StopWriterThread()
        {
            _pulseQueue?.CompleteAdding();

            if (WriterThreadExistsAndIsAlive)
            {
                if (!_writerThread.Join(1000))
                {
                    Debug.LogWarning("Writer thread failed to stop");
                }
            }

            _pulseQueue?.Dispose();
            _pulseQueue = null;
            _writerThread = null;
        }


        private void WritePulsesFromQueue()
        {
            try
            {
                foreach (byte value in _pulseQueue.GetConsumingEnumerable())
                {
                    WritePulse(value, PulseWidth);
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
        }

        private void WritePulse(byte value, int delayMilliseconds)
        {
            SendByte(value);
            if (delayMilliseconds > 0) Thread.Sleep(delayMilliseconds);
            SendByte(0);
        }
    }
}