using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        class TableRates
        {
            [JsonPropertyName("table")]
            public string Table { get; set; }

            [JsonPropertyName("no")]
            public string Number { get; set; }

            [JsonPropertyName("tradingDate")]
            public DateTime TradingDate { get; set; }

            [JsonPropertyName("effectiveDate")]
            public DateTime EffectiveDate { get; set; }

            [JsonPropertyName("rates")]
            public List<Rate> Rates { get; set; }
        }

        record Rate
        {
            [JsonPropertyName("currency")]
            public string Currency { get; set; }

            [JsonPropertyName("code")]
            public string Code { get; set; }

            [JsonPropertyName("ask")]
            public decimal Ask { get; set; }

            [JsonPropertyName("bid")]
            public decimal Bid { get; set; }

            public Rate(string Currency, string Code, decimal Ask, decimal Bid)
            {
                this.Currency = Currency;
                this.Code = Code;
                this.Bid = Bid;
                this.Ask = Ask;
            }
            public Rate()
            {
                //
            }
        }

        Dictionary<string, Rate> Rates = new Dictionary<string, Rate>();
        private void DownloadJsonData()
        {
            WebClient client = new WebClient();
            client.Headers.Add("Accept", "application/json");
            string json = client.DownloadString("http://api.nbp.pl/api/exchangerates/tables/C");
            
            List<TableRates> tableRates = JsonSerializer.Deserialize<List<TableRates>>(json);
            TableRates table = tableRates[0];
            table.Rates.Add(new Rate() { Currency = "złoty", Code = "PLN", Ask = 1, Bid = 1 });

            foreach (var rate in table.Rates)
            {
                Rates.Add(rate.Code, rate);
            }
        }

        private void DownloadData()
        {
            CultureInfo info = CultureInfo.CreateSpecificCulture("en-EN");
            WebClient client = new WebClient();
            client.Headers.Add("Accept", "application/xml");
            string xmlRate = client.DownloadString("http://api.nbp.pl/api/exchangerates/tables/C");
            XDocument rateDoc = XDocument.Parse(xmlRate);
            
            var rates = rateDoc
                .Element("ArrayOfExchangeRatesTable")
                .Elements("ExchangeRatesTable")
                .Elements("Rates")
                .Elements("Rate")
                .Select(x => 
                new Rate(x.Element("Currency").Value,
                         x.Element("Code").Value,
                         decimal.Parse(x.Element("Ask").Value, info),
                         decimal.Parse(x.Element("Bid").Value, info)));

            foreach (var rate in rates)
            {
                Rates.Add(rate.Code, rate);
            }
            Rates.Add("PLN", new Rate("złoty", "PLN", 1, 1));
        }

        public MainWindow()
        {
            InitializeComponent();
            DownloadJsonData();
            UpdateGUI();
        }

        private void UpdateGUI()
        {
            OutputCurrencyCode.Items.Clear();
            InputCurrencyCode.Items.Clear();
            foreach (var code in Rates.Keys)
            {
                OutputCurrencyCode.Items.Add(code);
                InputCurrencyCode.Items.Add(code);
            }

            OutputCurrencyCode.SelectedIndex = 0;
            InputCurrencyCode.SelectedIndex = 1;
        }

        private void CalcResult(object sender, RoutedEventArgs e)
        {
            string outputCode;
            outputCode = OutputCurrencyCode.Text;

            Rate inputRate = Rates[InputCurrencyCode.Text];
            Rate outputRate = Rates[OutputCurrencyCode.Text];
            
            if (decimal.TryParse(InputAmount.Text, out decimal amount))
            {
                decimal result = amount * inputRate.Ask / outputRate.Ask;
                OutputAmount.Text = result.ToString("N2") + " " + outputCode;
            }
        }

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Wybierz plik:";
            dialog.Filter = "Pliki tekstowe (*.txt)|*.txt|Wszystkie pliki (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                if (File.Exists(dialog.FileName))
                {
                    string[] lines = File.ReadAllLines(dialog.FileName);
                    Rates.Clear();
                    foreach (var line in lines)
                    {
                        string[] tokens = line.Split(";");

                        string code = tokens[0];
                        string currency = tokens[1];
                        string askStr = tokens[2];
                        string bidStr = tokens[3];

                        if (decimal.TryParse(askStr, out decimal ask) && decimal.TryParse(bidStr, out decimal bid))
                        {
                            Rate rate = new Rate() { Code = code, Currency = currency, Ask = ask, Bid = bid };
                            Rates.Add(rate.Code, rate);
                        }
                    }
                    UpdateGUI();
                }
            }
        }

        private void SaveFileJson_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Pliki JSON (*.json)|*.json";
            saveFileDialog.Title = "Zapisz plik:";

            if (saveFileDialog.ShowDialog() == true)
            {
                string json = JsonSerializer.Serialize(Rates);
                File.WriteAllText(saveFileDialog.FileName, json);
            }
        }

        private void LoadFileJson_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Wybierz plik:";
            dialog.Filter = "Pliki json (*.json)|*.json|Wszystkie pliki (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                string content = File.ReadAllText(dialog.FileName);
                Rates = JsonSerializer.Deserialize<Dictionary<string, Rate>>(content);
                UpdateGUI();
            }
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            string oldText = InputAmount.Text;
            string deltaText = e.Text;

            e.Handled = !decimal.TryParse(oldText + deltaText, out decimal value);
        }
    }
}
