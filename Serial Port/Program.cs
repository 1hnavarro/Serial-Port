using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.IO.Ports;
using SuperWebSocket;

namespace Serial_Port
{
    class Program
    {
        static void Main(string[] args)
        {
            WebSocketServer wsServer;
            List<WebSocketSession> connections = new List<WebSocketSession>();

            wsServer = new WebSocketServer();
            wsServer.Setup(1337);
            wsServer.NewSessionConnected += WsServer_NewSessionConnected;
            wsServer.NewMessageReceived += WsServer_NewMessageReceived;
            wsServer.NewDataReceived += WsServer_NewDataReceived;
            wsServer.SessionClosed += WsServer_SessionClosed;

            wsServer.Start();

            void ReadSerialPort()
            {
                void Send(String value)
                {
                    List<WebSocketSession> _connections;
                    _connections = new List<WebSocketSession>(connections);
                    foreach (WebSocketSession connection in _connections)
                        connection.Send(value);
                }

                SerialPort _serialPort;
                _serialPort = new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One);
                _serialPort.BaudRate = 9600;
                _serialPort.DataBits = 8;
                _serialPort.ReadTimeout = 10000;
                _serialPort.NewLine = "\r";

                while (true)
                {
                    try
                    {
                        if (!_serialPort.IsOpen)
                        {
                            _serialPort.Open();
                            Send("{ \"type\": false, \"message\": \"Connecting\"}");
                        }

                        while (_serialPort.IsOpen)
                        {
                            try
                            {
                                Send("{ \"type\": true, \"message\": \"" + _serialPort.ReadLine() + "\"}");
                                Thread.Sleep(500);
                            }
                            catch (System.TimeoutException)
                            {
                                Send("{ \"type\": false, \"message\": \"Time Out\"}");
                                Thread.Sleep(2000);
                                break;
                            }
                        }
                    }
                    catch
                    {
                        Send("{ \"type\": false, \"message\": \"Port COM3 not open\"}");
                        _serialPort.Close();
                        Thread.Sleep(10000);
                    }
                }
            }

            Thread sPort = new Thread(ReadSerialPort);
            sPort.Start();

            void WsServer_SessionClosed(WebSocketSession session, SuperSocket.SocketBase.CloseReason value)
            {
                connections.Remove(session);
            }
            void WsServer_NewDataReceived(WebSocketSession session, byte[] value)
            {
                Console.WriteLine("NewDataReceived");
            }
            void WsServer_NewMessageReceived(WebSocketSession session, string value)
            {
                foreach (WebSocketSession connection in connections)
                    connection.Send("Hola -> " + value);
            }
            void WsServer_NewSessionConnected(WebSocketSession session)
            {
                connections.Add(session);
                Console.WriteLine("NewSessionConnected");
            }
        }
    }
}
