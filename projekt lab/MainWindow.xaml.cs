using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace projekt_lab
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        record Rate(string Currency, string Code, decimal Ask, decimal Bid);
        Dictionary<string, Rate> Rates = new Dictionary<string, Rate>();

        private void DownloadData()
        {
            WebClient client = new WebClient();
            client.Headers.Add("Accept", "application/xml");
            string xmlRate = client.DownloadString("http://api.nbp.pl/api/exchangerates/tables/C");
            XDocument rateDoc = XDocument.Parse(xmlRate);
            var rates = rateDoc
                .Element("ArrayOfExchangeRatesTable")
                .Elements("EchangeRatesTable")
                .Elements("Rates")
                .Elements("Rate")
                .Select(x => new Rate(
                    x.Element("Currency").Value,
                    x.Element("Code").Value,
                    decimal.Parse(x.Element("Ask").Value),
                    decimal.Parse(x.Element("Bid").Value)
                    ));
        }
        public MainWindow()
        {
            InitializeComponent();
            OutputCurrencyCode.Items.Add("USD");
            OutputCurrencyCode.Items.Add("PLN");
            OutputCurrencyCode.Items.Add("EUR");

            InputCurrencyCode.Items.Add("USD");
            InputCurrencyCode.Items.Add("PLN");
            InputCurrencyCode.Items.Add("EUR");

            OutputCurrencyCode.SelectedIndex = 0;
            InputCurrencyCode.SelectedIndex = 1;
        }

        private void CalcResult(object sender, RoutedEventArgs e)
        {
            OutputAmount.Text = "99999";
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            string oldText = InputAmount.Text;
            string deltaText = e.Text;
            e.Handled = !decimal.TryParse(oldText + deltaText, out decimal value);
        }
    }
}
