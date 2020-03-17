/*  CTRADER GURU --> Template 1.0.3

    Homepage    : https://ctrader.guru/
    Telegram    : https://t.me/ctraderguru
    Twitter     : https://twitter.com/cTraderGURU/
    Facebook    : https://www.facebook.com/ctrader.guru/
    YouTube     : https://www.youtube.com/channel/UCKkgbw09Fifj65W5t5lHeCQ
    GitHub      : https://github.com/cTraderGURU/
    TOS         : https://ctrader.guru/termini-del-servizio/

*/

using System;
using cAlgo.API;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

// --> Microsoft Visual Studio 2017 --> Strumenti --> Gestione pacchetti NuGet --> Gestisci pacchetti NuGet per la soluzione... --> Installa
using Newtonsoft.Json;

#region Extensions

/// <summary>
/// Estensione che fornisce dei metodi peculiari del cbot stesso
/// </summary>
public static class ChartTrendLineExtensions
{

    private static readonly Color ActivateLongColor = Color.DodgerBlue;
    private static readonly Color ActivateShortColor = Color.Red;
    private static readonly Color DeliveredColor = Color.Gray;

    /// <summary>
    /// Aggiorna lo stato delle trendline con breakout dal basso verso l'alto
    /// </summary>
    /// <param name="MyTrendLine">La trendline da modificare</param>
    public static void ToOver(this ChartTrendLine MyTrendLine)
    {

        MyTrendLine.Color = ActivateLongColor;
        MyTrendLine.Thickness = 2;
        MyTrendLine.LineStyle = LineStyle.Solid;
        MyTrendLine.ExtendToInfinity = true;
        MyTrendLine.IsInteractive = true;

    }

    /// <summary>
    /// Aggiorna lo stato delle trendline con breakout dall'alto verso il basso
    /// </summary>
    /// <param name="MyTrendLine">La trendline da modificare</param>
    public static void ToUnder(this ChartTrendLine MyTrendLine)
    {

        MyTrendLine.Color = ActivateShortColor;
        MyTrendLine.Thickness = 2;
        MyTrendLine.LineStyle = LineStyle.Solid;
        MyTrendLine.ExtendToInfinity = true;
        MyTrendLine.IsInteractive = true;

    }

    /// <summary>
    /// Aggiorna lo stato delle trendline che hanno adempiuto al proprio dovere
    /// </summary>
    /// <param name="MyTrendLine">La trendline da modificare</param>
    public static void ToDelivered(this ChartTrendLine MyTrendLine)
    {

        MyTrendLine.Comment = "delivered";
        MyTrendLine.Color = DeliveredColor;
        MyTrendLine.Thickness = 1;
        MyTrendLine.LineStyle = LineStyle.DotsRare;
        MyTrendLine.ExtendToInfinity = true;

    }

}

#endregion

namespace cAlgo.Robots
{

    // --> AccessRights = AccessRights.FullAccess se si vuole controllare gli aggiornamenti
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class TrendLinePower : Robot
    {

        #region Enums & Class

        /// <summary>
        /// Definisce le tipologie di breakout che la trendline deve osservare
        /// </summary>
        public enum _BreakOutType
        {

            BreakOut,
            BreakOutBar,
            Disabled

        }

        /// <summary>
        /// Definisce le tipologie di rimozione ordini pendenti che la trendline deve osservare e degli alerts
        /// </summary>
        public enum _ActionType
        {

            Open,
            Close,
            All,
            Disabled

        }

        #endregion

        #region Identity

        /// <summary>
        /// ID prodotto, identificativo, viene fornito da ctrader.guru
        /// </summary>
        public const int ID = 75166;

        /// <summary>
        /// Nome del prodotto, identificativo, da modificare con il nome della propria creazione
        /// </summary>
        public const string NAME = "Trendline Power";

        /// <summary>
        /// La versione del prodotto, progressivo, utilie per controllare gli aggiornamenti se viene reso disponibile sul sito ctrader.guru
        /// </summary>
        public const string VERSION = "1.0.0";

        #endregion

        #region Params

        [Parameter(NAME + " " + VERSION, Group = "Identity", DefaultValue = "https://ctrader.guru/product/trendline-power/")]
        public string ProductInfo { get; set; }

        [Parameter("Label ( Magic Name )", Group = "Identity", DefaultValue = NAME)]
        public string MyLabel { get; set; }

        [Parameter("Open", Group = "Options", DefaultValue = _BreakOutType.BreakOutBar)]
        public _BreakOutType MyOpenMode { get; set; }

        [Parameter("Close", Group = "Options", DefaultValue = _BreakOutType.BreakOutBar)]
        public _BreakOutType MyCloseMode { get; set; }

        [Parameter("Remove Pending", Group = "Options", DefaultValue = _ActionType.All)]
        public _ActionType MyPendingMode { get; set; }

        [Parameter("Alert", Group = "Options", DefaultValue = _ActionType.All)]
        public _ActionType MyAlertMode { get; set; }

        [Parameter("Lots", Group = "Money Management", DefaultValue = 0.01, MinValue = 0.01, Step = 0.01)]
        public double MyLots { get; set; }

        [Parameter("Stop Loss", Group = "Money Management", DefaultValue = 30, MinValue = 0, Step = 0.1)]
        public double SL { get; set; }

        [Parameter("Take Profit", Group = "Money Management", DefaultValue = 30, MinValue = 0, Step = 0.1)]
        public double TP { get; set; }

        [Parameter("Slippage", Group = "Money Management", DefaultValue = 2, MinValue = 0.1, Step = 0.1)]
        public double MySlippage { get; set; }

        [Parameter("Max Spread allowed", Group = "Filters", DefaultValue = 1.5, MinValue = 0.1, Step = 0.1)]
        public double SpreadToTrigger { get; set; }

        #endregion

        #region Property

        bool AlertInThisBar = false;

        #endregion

        #region cBot Events

        protected override void OnStart()
        {

            // --> Stampo nei log la versione corrente
            Print("{0} : {1}", NAME, VERSION);

            // --> Se viene settato l'ID effettua un controllo per verificare eventuali aggiornamenti
            _checkProductUpdate();

        }

        protected override void OnTick()
        {

            // --> Controllo lo stato delle trendlines e le relative azioni da intraprendere
            _checkTrendLines();

        }

        protected override void OnBar()
        {

            // --> Resetto il flag per la candela
            AlertInThisBar = false;

        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Passa al setaccio tutte le trendlines per adeguarle alle intenzioni
        /// </summary>
        private void _checkTrendLines()
        {

            // --> Prelevo le trendline
            ChartTrendLine[] alltrendlines = Chart.FindAllObjects<ChartTrendLine>();

            // --> Controllo una alla volta il dafarsi
            foreach (ChartTrendLine myline in alltrendlines)
            {

                // --> Se è non inizializzata ci penso io
                if (myline.Comment == null)
                    myline.Comment = "";

                // --> Utilizziamo i commenti per individuare i comandi
                switch (myline.Comment.ToLower())
                {

                    // --> Vecchia trendline, può essere rimossa ma meglio lasciarla come storico sul grafico
                    case "delivered":

                        // --> Chart.RemoveObject(myline.Name);

                        break;

                    case "over":

                        // --> Cambio lo stile della trendline
                        myline.ToOver();

                        // --> Eseguo operazioni di controllo prima per le chiusure e poi per le aperture
                        _checkActionCloseOver(myline);
                        _checkActionOpenOver(myline);

                        break;

                    case "under":

                        // --> Stesse operazioni per Over ma in questo caso per Under
                        myline.ToUnder();
                        _checkActionCloseUnder(myline);
                        _checkActionOpenUnder(myline);

                        break;
                    default:


                        // --> Prelevo il prezzo corrente della trendline di riferimento
                        double price = Math.Round(myline.CalculateY(Chart.BarsTotal - 1), Symbol.Digits);

                        // --> Recepisco il comando
                        if (price > Ask)
                        {

                            myline.Comment = "over";

                        }
                        else if (price < Bid)
                        {

                            myline.Comment = "under";

                        }


                        break;

                }

            }

        }

        /// <summary>
        /// Stampa informazioni nei log
        /// </summary>
        /// <param name="mex">Il messaggio da registrare nei log</param>
        private void _log(string mex)
        {

            if (RunningMode != RunningMode.RealTime)
                return;

            Print("{0} : {1}", NAME, mex);

        }

        /// <summary>
        /// Controlla lo stato del breakout OVER a seconda delle opzioni scelte
        /// </summary>
        /// <param name="myline">Le trendline da controllare</param>
        /// <param name="mybreakout">Il tipo di breakout da considerare</param>
        /// <returns>Restituisce true se siamo in presenza di un breakout</returns>
        private bool _haveBreakOutOver(ChartTrendLine myline, _BreakOutType mybreakout)
        {

            // --> Se è disabilitato è inutile proseguire
            if (mybreakout == _BreakOutType.Disabled)
                return false;

            // --> Recupero il prezzo corrispondente alla trendline
            double lineprice = Math.Round(myline.CalculateY(Chart.BarsTotal - 1), Symbol.Digits);

            // --> Decidiamo cosa fare a seconda della condizione delle candele
            switch (mybreakout)
            {

                // --> Semplice breakout non appena supera la linea di trend
                case _BreakOutType.BreakOut:

                    return (Ask > lineprice);

                // --> Aspettiamo la chiusura della candela e ad ogni modo abbiamo una candela di tempo per chiudere tutto
                case _BreakOutType.BreakOutBar:

                    // --> Devo avere l'apertura della candela corrente sopra la linea di trend e l'apertura della precedente sotto, potrebbe esserci un GAP
                    return (Bars.LastBar.Open > lineprice && Bars.Last(1).Open < lineprice);


            }

            return false;

        }

        /// <summary>
        /// Controlla lo stato del breakout UNDER a seconda delle opzioni scelte
        /// </summary>
        /// <param name="myline">Le trendline da controllare</param>
        /// <param name="mybreakout">Il tipo di breakout da considerare</param>
        /// <returns>Restituisce true se siamo in presenza di un breakout</returns>
        private bool _haveBreakOutUnder(ChartTrendLine myline, _BreakOutType mybreakout)
        {

            // --> Se è disabilitato è inutile proseguire
            if (mybreakout == _BreakOutType.Disabled)
                return false;

            // --> Recupero il prezzo corrispondente alla trendline
            double lineprice = Math.Round(myline.CalculateY(Chart.BarsTotal - 1), Symbol.Digits);

            // --> Decidiamo cosa fare a seconda della condizione delle candele
            switch (mybreakout)
            {

                // --> Semplice breakout non appena supera la linea di trend
                case _BreakOutType.BreakOut:

                    return (Bid < lineprice);

                // --> Aspettiamo la chiusura della candela e ad ogni modo abbiamo una candela di tempo per chiudere tutto
                case _BreakOutType.BreakOutBar:

                    // --> Devo avere l'apertura della candela corrente sotto la linea di trend e l'apertura della precedente sopra, potrebbe esserci un GAP
                    return (Bars.LastBar.Open < lineprice && Bars.Last(1).Open > lineprice);

            }

            return false;

        }

        /// <summary>
        /// Gestisce le popup per gli alert
        /// </summary>
        private void _alert()
        {

            if (RunningMode != RunningMode.RealTime || AlertInThisBar)
                return;

            string mex = string.Format("{0} : {1} breakout, Ask {2} / Bid {3}", NAME, SymbolName, Ask.ToString(), Bid.ToString());

            AlertInThisBar = true;

            // --> La popup non deve interrompere la logica delle API, apertura e chiusura

            new Thread(new ThreadStart(delegate { MessageBox.Show(mex, "BreakOut", MessageBoxButtons.OK, MessageBoxIcon.Information); })).Start();


        }

        /// <summary>
        /// Controllo le condizioni di apertura alla rottura della trend line
        /// </summary>
        /// <param name="myline">La trendline da esaminare</param>
        private void _checkActionOpenOver(ChartTrendLine myline)
        {

            // --> Solo breakout quindi se diversamente predisposto esco, anche se non sono in condizioni di prezzo giuste
            if (!_haveBreakOutOver(myline, MyOpenMode))
                return;

            // --> Potrebbero esserci allargamenti di spread
            if (Symbol.Spread < SpreadToTrigger)
            {

                var volumeInUnits = Symbol.QuantityToVolumeInUnits(MyLots);

                TradeResult result = ExecuteMarketRangeOrder(TradeType.Buy, Symbol.Name, volumeInUnits, MySlippage, Ask, MyLabel, SL, TP);

                if (!result.IsSuccessful)
                    _log("can't open new order buy (" + result.Error + ")");

            }
            else
            {

                // --> Meglio registrate quanto accaduto per una indagine successiva
                _log("max spread hit " + Symbol.Spread.ToString("N1"));

            }

            if (MyPendingMode == _ActionType.All || MyPendingMode == _ActionType.Open)
                _basketRemovePending();

            if (MyAlertMode == _ActionType.All || MyAlertMode == _ActionType.Open)
                _alert();

            // --> comunque vada rendo inefficace la trendline
            myline.ToDelivered();

        }

        /// <summary>
        /// Controllo le condizioni di chiusura alla rottura della trend line
        /// </summary>
        /// <param name="myline">La trendline da esaminare</param>
        private void _checkActionCloseOver(ChartTrendLine myline)
        {

            // --> Solo breakout quindi se diversamente predisposto esco, anche se non sono in condizioni di prezzo giuste
            if (!_haveBreakOutOver(myline, MyCloseMode))
                return;

            _basketClose();

            if (MyPendingMode == _ActionType.All || MyPendingMode == _ActionType.Close)
                _basketRemovePending();

            if (MyAlertMode == _ActionType.All || MyAlertMode == _ActionType.Close)
                _alert();

            // --> comunque vada rendo inefficace la trendline
            myline.ToDelivered();

        }

        /// <summary>
        /// Controllo le condizioni di apertura alla rottura della trend line
        /// </summary>
        /// <param name="myline">La trendline da esaminare</param>
        private void _checkActionOpenUnder(ChartTrendLine myline)
        {

            // --> Solo breakout quindi se diversamente predisposto esco, anche se non sono in condizioni di prezzo giuste
            if (!_haveBreakOutUnder(myline, MyOpenMode))
                return;

            // --> Potrebbero esserci allargamenti di spread
            if (Symbol.Spread < SpreadToTrigger)
            {

                var volumeInUnits = Symbol.QuantityToVolumeInUnits(MyLots);

                TradeResult result = ExecuteMarketRangeOrder(TradeType.Sell, Symbol.Name, volumeInUnits, MySlippage, Bid, MyLabel, SL, TP);

                if (!result.IsSuccessful)
                    _log("can't open new order sell (" + result.Error + ")");

            }
            else
            {

                // --> Meglio registrate quanto accaduto per una indagine successiva
                _log("max spread hit " + Symbol.Spread.ToString("N1"));

            }

            if (MyPendingMode == _ActionType.All || MyPendingMode == _ActionType.Open)
                _basketRemovePending();

            if (MyAlertMode == _ActionType.All || MyAlertMode == _ActionType.Open)
                _alert();

            // --> comunque vada rendo inefficace la trendline
            myline.ToDelivered();

        }

        /// <summary>
        /// Controllo le condizioni di chiusura alla rottura della trend line
        /// </summary>
        /// <param name="myline">La trendline da esaminare</param>
        private void _checkActionCloseUnder(ChartTrendLine myline)
        {

            // --> Solo breakout quindi se diversamente predisposto esco, anche se non sono in condizioni di prezzo giuste
            if (!_haveBreakOutUnder(myline, MyCloseMode))
                return;

            _basketClose();

            if (MyPendingMode == _ActionType.All || MyPendingMode == _ActionType.Close)
                _basketRemovePending();

            if (MyAlertMode == _ActionType.All || MyAlertMode == _ActionType.Close)
                _alert();

            // --> comunque vada rendo inefficace la trendline
            myline.ToDelivered();

        }

        /// <summary>
        /// Effettua un controllo sul sito ctrader.guru per mezzo delle API per verificare la presenza di aggiornamenti, solo in realtime
        /// </summary>
        private void _checkProductUpdate()
        {

            // --> Controllo solo se solo in realtime, evito le chiamate in backtest
            if (RunningMode != RunningMode.RealTime)
                return;

            // --> Organizzo i dati per la richiesta degli aggiornamenti
            Guru.API.RequestProductInfo Request = new Guru.API.RequestProductInfo 
            {

                MyProduct = new Guru.Product 
                {

                    ID = ID,
                    Name = NAME,
                    Version = VERSION

                },
                AccountBroker = Account.BrokerName,
                AccountNumber = Account.Number

            };

            // --> Effettuo la richiesta
            Guru.API Response = new Guru.API(Request);

            // --> Controllo per prima cosa la presenza di errori di comunicazioni
            if (Response.ProductInfo.Exception != "")
            {

                Print("{0} Exception : {1}", NAME, Response.ProductInfo.Exception);

            }
            // --> Chiedo conferma della presenza di nuovi aggiornamenti
            else if (Response.HaveNewUpdate())
            {

                string updatemex = string.Format("{0} : Updates available {1} ( {2} )", NAME, Response.ProductInfo.LastProduct.Version, Response.ProductInfo.LastProduct.Updated);

                // --> Informo l'utente con un messaggio sul grafico e nei log del cbot
                Chart.DrawStaticText(NAME + "Updates", updatemex, API.VerticalAlignment.Top, API.HorizontalAlignment.Left, Color.Red);
                Print(updatemex);

            }

        }

        private void _basketClose()
        {

            // --> Chiudo tutti i trade di questo simbolo
            foreach (var position in Positions)
            {

                if (position.SymbolName != SymbolName)
                    continue;

                ClosePositionAsync(position);

            }

        }

        private void _basketRemovePending()
        {

            foreach (var order in PendingOrders)
            {

                if (order.SymbolName != SymbolName)
                    continue;

                CancelPendingOrderAsync(order);

            }

        }

        #endregion

    }

}

/// <summary>
/// NameSpace che racchiude tutte le feature ctrader.guru
/// </summary>
namespace Guru
{
    /// <summary>
    /// Classe che definisce lo standard identificativo del prodotto nel marketplace ctrader.guru
    /// </summary>
    public class Product
    {

        public int ID = 0;
        public string Name = "";
        public string Version = "";
        public string Updated = "";

    }

    /// <summary>
    /// Offre la possibilità di utilizzare le API messe a disposizione da ctrader.guru per verificare gli aggiornamenti del prodotto.
    /// Permessi utente "AccessRights = AccessRights.FullAccess" per accedere a internet ed utilizzare JSON
    /// </summary>
    public class API
    {
        /// <summary>
        /// Costante da non modificare, corrisponde alla pagina dei servizi API
        /// </summary>
        private const string Service = "https://ctrader.guru/api/product_info/";

        /// <summary>
        /// Costante da non modificare, utilizzata per filtrare le richieste
        /// </summary>
        private const string UserAgent = "cTrader Guru";

        /// <summary>
        /// Variabile dove verranno inserite le direttive per la richiesta
        /// </summary>
        private RequestProductInfo RequestProduct = new RequestProductInfo();

        /// <summary>
        /// Variabile dove verranno inserite le informazioni identificative dal server dopo l'inizializzazione della classe API
        /// </summary>
        public ResponseProductInfo ProductInfo = new ResponseProductInfo();

        /// <summary>
        /// Classe che formalizza i parametri di richiesta, vengono inviate le informazioni del prodotto e di profilazione a fini statistici
        /// </summary>
        public class RequestProductInfo
        {

            /// <summary>
            /// Il prodotto corrente per il quale richiediamo le informazioni
            /// </summary>
            public Product MyProduct = new Product();

            /// <summary>
            /// Broker con il quale effettiamo la richiesta
            /// </summary>
            public string AccountBroker = "";

            /// <summary>
            /// Il numero di conto con il quale chiediamo le informazioni
            /// </summary>
            public int AccountNumber = 0;

        }

        /// <summary>
        /// Classe che formalizza lo standard per identificare le informazioni del prodotto
        /// </summary>
        public class ResponseProductInfo
        {

            /// <summary>
            /// Il prodotto corrente per il quale vengono fornite le informazioni
            /// </summary>
            public Product LastProduct = new Product();

            /// <summary>
            /// Eccezioni in fase di richiesta al server, da utilizzare per controllare l'esito della comunicazione
            /// </summary>
            public string Exception = "";

            /// <summary>
            /// La risposta del server
            /// </summary>
            public string Source = "";

        }

        /// <summary>
        /// Richiede le informazioni del prodotto richiesto
        /// </summary>
        /// <param name="Request"></param>
        public API(RequestProductInfo Request)
        {

            RequestProduct = Request;

            // --> Non controllo se non ho l'ID del prodotto
            if (Request.MyProduct.ID <= 0)
                return;

            // --> Dobbiamo supervisionare la chiamata per registrare l'eccexione
            try
            {

                // --> Strutturo le informazioni per la richiesta POST
                NameValueCollection data = new NameValueCollection 
                {
                    {
                        "account_broker",
                        Request.AccountBroker
                    },
                    {
                        "account_number",
                        Request.AccountNumber.ToString()
                    },
                    {
                        "my_version",
                        Request.MyProduct.Version
                    },
                    {
                        "productid",
                        Request.MyProduct.ID.ToString()
                    }
                };

                // --> Autorizzo tutte le pagine di questo dominio
                Uri myuri = new Uri(Service);
                string pattern = string.Format("{0}://{1}/.*", myuri.Scheme, myuri.Host);

                Regex urlRegEx = new Regex(pattern);
                WebPermission p = new WebPermission(NetworkAccess.Connect, urlRegEx);
                p.Assert();

                // --> Protocollo di sicurezza https://
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;

                // -->> Richiedo le informazioni al server
                using (var wb = new WebClient())
                {

                    wb.Headers.Add("User-Agent", UserAgent);

                    var response = wb.UploadValues(myuri, "POST", data);
                    ProductInfo.Source = Encoding.UTF8.GetString(response);

                }

                // -->>> Nel cBot necessita l'attivazione di "AccessRights = AccessRights.FullAccess"
                ProductInfo.LastProduct = JsonConvert.DeserializeObject<Product>(ProductInfo.Source);

            } catch (Exception Exp)
            {

                // --> Qualcosa è andato storto, registro l'eccezione
                ProductInfo.Exception = Exp.Message;

            }

        }

        /// <summary>
        /// Esegue un confronto tra le versioni per determinare la presenza di aggiornamenti
        /// </summary>
        /// <returns></returns>
        public bool HaveNewUpdate()
        {

            // --> Voglio essere sicuro che stiamo lavorando con le informazioni giuste
            return (ProductInfo.LastProduct.ID == RequestProduct.MyProduct.ID && ProductInfo.LastProduct.Version != "" && RequestProduct.MyProduct.Version != "" && new Version(RequestProduct.MyProduct.Version).CompareTo(new Version(ProductInfo.LastProduct.Version)) < 0);

        }

    }

}
