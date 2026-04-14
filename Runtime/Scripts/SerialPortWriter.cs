using System;
using System.IO.Ports;
using UnityEngine;


namespace BCIEssentials.SerialTriggers
{
    public class SerialPortWriter
    {
        public bool IsConnected => _port?.IsOpen == true;
        public bool PrintLogs;
        public int MaximumConsecutiveWriteErrors = 2;

        private SerialPort _port;
        private readonly byte[] _writeBuffer = new byte[1];
        private int _consecutiveWriteErrors;


        public void Connect
        (
            string portName, int baudRate = 9600,
            int writeTimeout = 500
        )
        => Connect(portName, baudRate, writeTimeout: writeTimeout);

        public void Connect
        (
            string portName, int baudRate = 9600,
            Parity parity = Parity.None,
            int dataBits = 8,
            StopBits stopBits = StopBits.One,
            int writeTimeout = 500
        )
        {
            if (IsConnected)
            {
                Debug.LogWarning($"Already connected");
                return;
            }

            SetUp();

            try
            {
                _port = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
                {
                    DtrEnable = true,
                    RtsEnable = false,
                    WriteTimeout = writeTimeout > 0 ? writeTimeout : -1,
                };
                _port.Open();
                _consecutiveWriteErrors = 0;
            }
            catch (UnauthorizedAccessException)
            {
                Debug.LogError(
                    $"SerialMarkerWriter: access denied for {portName}. "
                    + "The port may be in use by another application."
                );
                DisposePort();
            }
        }

        public void Disconnect()
        {
            CleanUp();

            if (IsConnected) SendByte(0);

            _port?.Close();
            DisposePort();
        }

        public void Reconnect()
        {
            Disconnect();
            Connect(
                _port.PortName, _port.BaudRate,
                _port.Parity, _port.DataBits,
                _port.StopBits, _port.WriteTimeout
            );
        }


        public virtual void SendByte(byte value)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("Cannot send, port not open.");
                return;
            }

            try
            {
                _writeBuffer[0] = value;
                _port.Write(_writeBuffer, 0, 1);
                _consecutiveWriteErrors = 0;
                if (PrintLogs)
                {
                    Debug.Log($"Wrote byte {value} (0x{value:X2}) to {_port.PortName}");
                }
            }
            catch (Exception ex) { HandleWriteError(ex.Message); }
        }


        private void HandleWriteError(string message)
        {
            _consecutiveWriteErrors++;
            Debug.LogError($"{message} (error {_consecutiveWriteErrors})");

            if (MaximumConsecutiveWriteErrors > 0 && _consecutiveWriteErrors >= MaximumConsecutiveWriteErrors)
            {
                Debug.LogError($"{_consecutiveWriteErrors} consecutive errors, disconnecting.");
                Disconnect();
            }
        }

        private void DisposePort()
        {
            _port?.Dispose();
            _port = null;
        }

        protected virtual void SetUp() { }
        protected virtual void CleanUp() { }
    }
}