using System.Windows;

namespace ArduinoLightController
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel { get; } = new MainViewModel();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = ViewModel;         
        }

        private void ConnectDisconnect_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ConnectOrDisconnect();
        }

        private void TurnOn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TurnOnLight();
        }

        private void TurnOff_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TurnOffLight();
        }
    }
}
