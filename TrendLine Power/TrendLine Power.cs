/*  CTRADER GURU --> Template 1.0.6

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
using System.Threading;
using System.Windows.Forms;

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
        /// Nome del prodotto, identificativo, da modificare con il nome della propria creazione
        /// </summary>
        public const string NAME = "Trendline Power";

        /// <summary>
        /// La versione del prodotto, progressivo, utilie per controllare gli aggiornamenti se viene reso disponibile sul sito ctrader.guru
        /// </summary>
        public const string VERSION = "1.0.1";

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
