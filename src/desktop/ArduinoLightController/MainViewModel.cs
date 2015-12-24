using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ArduinoLightController
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public List<string> Ports { get; } = new List<string>
        {
            "COM1",
            "COM2",
            "COM3",
            "COM4",
            "COM5",
            "COM6"
        };

        public enum Statuses
        {
            Disconnected,
            WaitingToConnect,
            LightOn,
            LightOff
        }
        
        private Statuses _status = Statuses.Disconnected;
        private string _connectAction = "Connect";

        private LightController _controller = new LightController();

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
            _controller.ConnectFailed += Controller_ConnectFailed;
            _controller.ConnectionChanged += Controller_ConnectionChanged;
            _controller.StatusUpdate += Controller_StatusUpdate;
        }

        private void Controller_StatusUpdate(object sender, LightController.LightStatus status)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                Status = status == LightController.LightStatus.On ? Statuses.LightOn : Statuses.LightOff;
            });
        }

        private void Controller_ConnectionChanged(object sender, bool isConnected)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(CanChangePort));
                Status = isConnected ? Statuses.WaitingToConnect : Statuses.Disconnected;
            });
        }

        private void Controller_ConnectFailed(object sender, Exception e)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                MessageBox.Show($"Connect failed: {e.GetType().Name} - {e.Message}");
            });
        }
                
        public void TurnOnLight()
        {
            _controller.SendCommand(LightController.Commands.TurnOn);
        }

        public void TurnOffLight()
        {
            _controller.SendCommand(LightController.Commands.TurnOff);
        }
        
        public void ConnectOrDisconnect()
        {
            if (_controller.IsConnected)
            {
                _controller.Disconnect();
            }
            else
            {
                _controller.Connect();
            }
        }

        public string SelectedPort
        {
            get { return _controller.PortName; }
            set
            {
                _controller.PortName = value;
                OnPropertyChanged(nameof(SelectedPort));
            }
        }

        public bool CanChangePort => !_controller.IsConnected;

        public bool CanSendCommands => _status == Statuses.LightOn || _status == Statuses.LightOff;

        public string StatusDescription
        {
            get
            {
                switch (_status)
                {
                    case Statuses.WaitingToConnect:
                        return "Waiting to connect...";
                    case Statuses.LightOn:
                        return "Light is ON";
                    case Statuses.LightOff:
                        return "Light is OFF";
                    default:
                        return _status.ToString();
                }
            }
        }

        public Statuses Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusDescription));
                OnPropertyChanged(nameof(CanSendCommands));
                OnPropertyChanged(nameof(ConnectAction));
            }
        }

        public string ConnectAction => _status == Statuses.Disconnected ? "Connect" : "Disconnect";

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
