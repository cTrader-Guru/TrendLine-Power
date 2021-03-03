/*  CTRADER GURU --> Template 1.0.6

    Homepage    : https://ctrader.guru/
    Telegram    : https://t.me/ctraderguru
    Twitter     : https://twitter.com/cTraderGURU/
    Facebook    : https://www.facebook.com/ctrader.guru/
    YouTube     : https://www.youtube.com/channel/UCKkgbw09Fifj65W5t5lHeCQ
    GitHub      : https://github.com/cTraderGURU/
    TOS         : https://ctrader.guru/termini-del-servizio/

    NOTE        : 

        Ho chiesto a questo indirizzo https://ctrader.com/forum/suggestions/23834
        di aggiungere una feature per accedere ai dati degli indicatori sul grafico,
        mi aspetto che tu voti questa richiesta per essere implementata nelle prossime
        versioni delle API.

        In _updateAllAreaEvents() ho commentato il codice che permette di osservare e gestire
        dal Form i dati, appena la richiesta sarà soddisfatta si potrà intervenire su _checkTrendLines()
        modificandolo per confrontare i dati degli indicatori.

*/

using System;
using cAlgo.API;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Net;

#region Extensions & Class

public static class Flag
{

    public const char Separator = '/';

    public const string DELIVERED = "delivered";

    public const string DISABLED = "di";

    public const string OpenBuyStop = "bs";
    public const string OpenBuyStopBar = "bb";
    public const string OpenBuyLimit = "bl";

    public const string OpenSellStop = "ss";
    public const string OpenSellStopBar = "sb";
    public const string OpenSellLimit = "sl";

    public const string Over = "ox";
    public const string OverBar = "ob";
    public const string Under = "ux";
    public const string UnderBar = "ub";

}

public static class ChartTrendLineExtensions
{

    private static readonly Color ColorBuy = Color.DodgerBlue;
    private static readonly Color ColorSell = Color.Red;
    private static readonly Color ColorClose = Color.Violet;
    private static readonly Color ColorAlert = Color.Orange;
    private static readonly Color ColorAllDisabled = Color.Gray;
    private static readonly Color ColorDelivered = Color.Gray;


    public static ChartTrendLine ToBuy(this ChartTrendLine MyTrendLine)
    {

        MyTrendLine.Color = ColorBuy;
        MyTrendLine.Thickness = 1;
        MyTrendLine.IsInteractive = true;

        return MyTrendLine;

    }

    public static ChartTrendLine ToSell(this ChartTrendLine MyTrendLine)
    {

        MyTrendLine.Color = ColorSell;
        MyTrendLine.Thickness = 1;
        MyTrendLine.IsInteractive = true;

        return MyTrendLine;

    }

    public static void ToAllDisabled(this ChartTrendLine MyTrendLine)
    {

        MyTrendLine.Color = ColorAllDisabled;
        MyTrendLine.LineStyle = LineStyle.DotsRare;
        MyTrendLine.Thickness = 1;

    }

    public static void ToDelivered(this ChartTrendLine MyTrendLine)
    {

        MyTrendLine.Comment = Flag.DELIVERED;
        MyTrendLine.Color = ColorDelivered;
        MyTrendLine.LineStyle = LineStyle.DotsRare;
        MyTrendLine.Thickness = 1;

    }

    public static void ToClose(this ChartTrendLine MyTrendLine)
    {

        MyTrendLine.Color = ColorClose;
        MyTrendLine.LineStyle = LineStyle.DotsRare;
        MyTrendLine.Thickness = 2;

    }

    public static void ToCloseBar(this ChartTrendLine MyTrendLine)
    {

        MyTrendLine.Color = ColorClose;
        MyTrendLine.LineStyle = LineStyle.DotsVeryRare;
        MyTrendLine.Thickness = 2;

    }

    public static void ToAlert(this ChartTrendLine MyTrendLine)
    {

        MyTrendLine.Color = ColorAlert;
        MyTrendLine.LineStyle = LineStyle.DotsRare;
        MyTrendLine.Thickness = 1;

    }

    public static void ToAlertBar(this ChartTrendLine MyTrendLine)
    {

        MyTrendLine.Color = ColorAlert;
        MyTrendLine.LineStyle = LineStyle.DotsVeryRare;
        MyTrendLine.Thickness = 1;

    }

    public static void Stop(this ChartTrendLine MyTrendLine)
    {

        MyTrendLine.LineStyle = LineStyle.DotsRare;

    }

    public static void StopBar(this ChartTrendLine MyTrendLine)
    {

        MyTrendLine.LineStyle = LineStyle.DotsVeryRare;

    }

    public static void Limit(this ChartTrendLine MyTrendLine)
    {

        MyTrendLine.LineStyle = LineStyle.Solid;

    }

}

#endregion

namespace cAlgo.Robots
{

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class TrendLinePower : Robot
    {

        #region Enums & Class

        public enum CurrentStateLine
        {

            Over,
            OverBar,
            Under,
            UnderBar,
            Undefined,
            OnGAP

        }

        public enum OnState
        {

            Tick,
            Bar

        }

        #endregion

        #region Identity

        public const string NAME = "Trendline Power";

        public const string VERSION = "2.1.3";

        public const string PAGE = "https://ctrader.guru/product/trendline-power/";

        #endregion

        #region Params

        /// <summary>
        /// Riferimenti del prodotto
        /// </summary>
        [Parameter(NAME + " " + VERSION, Group = "Identity", DefaultValue = PAGE)]
        public string ProductInfo { get; set; }

        /// <summary>
        /// Label che contraddistingue una operazione
        /// </summary>
        [Parameter("Label ( Magic Name )", Group = "Identity", DefaultValue = NAME)]
        public string MyLabel { get; set; }

        /// <summary>
        /// Decide cosa fare in GAP sulla linea
        /// </summary>
        [Parameter("Max GAP between Bars (pips, over = disable) :", Group = "Strategy", DefaultValue = 3)]
        public double MaxGAPBar { get; set; }

        [Parameter("Enabled?", Group = "Webhook", DefaultValue = false)]
        public bool WebhookEnabled { get; set; }

        [Parameter("API", Group = "Webhook", DefaultValue = "https://api.telegram.org/bot[ YOUR TOKEN ]/sendMessage")]
        public string Webhook { get; set; }

        [Parameter("POST params", Group = "Webhook", DefaultValue = "chat_id=[ @CHATID ]&text={0}")]
        public string PostParams { get; set; }

        #endregion

        #region Property

        bool CanDraw = false;
        Thread ThreadForm;
        FrmWrapper FormTrendLineOptions;

        private ChartObject TrendLineSelected = null;
        #endregion

        #region cBot Events

        protected override void OnStart()
        {

            // --> Con questo evitiamo errori comuni in backtest
            CanDraw = RunningMode == RunningMode.RealTime || RunningMode == RunningMode.VisualBacktesting;

            // --> Stampo nei log la versione corrente
            _log(string.Format("{0} {1}", VERSION, PAGE));

            // --> Avverto le condizioni operative
            _log("Press CTRL + Select Trendline");

            // --> Ad ogni aggiunta di oggetti resetto le trendline style
            Chart.ObjectsAdded += _delegateChartAdded;

            // --> Ogni volta che si inserisce una nuova area aggiorno tutto
            Chart.IndicatorAreaAdded += _areaAdded;

            // --> Aggiorno le aree da monitorare
            _updateAllAreaEvents();

        }

        protected override void OnStop()
        {

            _closeFormTrendLine();

        }

        protected override void OnTick()
        {

            // --> Controllo lo stato delle trendlines e le relative azioni da intraprendere
            _checkTrendLines(OnState.Tick);

        }

        protected override void OnBar()
        {

            // --> Controllo lo stato delle trendlines e le relative azioni da intraprendere
            _checkTrendLines(OnState.Bar);

        }

        #endregion

        #region Private Methods

        private void _checkTrendLines(OnState mystate, ChartTrendLine OneLine = null)
        {

            if (OneLine != null)
            {

                _manageTrendLine(mystate, OneLine);
                return;

            }

            // --> Prelevo le trendline dal grafico generale
            ChartTrendLine[] alltrendlines = Chart.FindAllObjects<ChartTrendLine>();

            // --> Le passo al setaccio
            foreach (ChartTrendLine myline in alltrendlines)
            {

                _manageTrendLine(mystate, myline);

            }

        }

        private void _manageTrendLine(OnState mystate, ChartTrendLine myline)
        {


            // --> Se non è inizializzata non devo fare nulla
            if (myline.Comment == null)
                return;

            // --> Potrebbe essere in un protoccollo non in linea con le aspettative
            string[] directive = myline.Comment.Split(Flag.Separator);

            // --> Aggiorno il feedback visivo
            if (!_checkFeedback(myline, directive))
                return;

            // --> Se la trendline non è infinita allora devo controllare il tempo, inizio con le scadute
            if (!myline.ExtendToInfinity && myline.Time1 < Bars.LastBar.OpenTime && myline.Time2 < Bars.LastBar.OpenTime)
            {

                myline.ToDelivered();
                return;

            }
            else if (myline.Time1 > Bars.LastBar.OpenTime && myline.Time2 > Bars.LastBar.OpenTime)
            {
                // --> Sono nel futuro, non opero
                return;

            }

            // --> Prelevo il prezzo della trendline
            double lineprice = Math.Round(myline.CalculateY(Chart.BarsTotal - 1), Symbol.Digits);

            switch (mystate)
            {

                // --> Solo controlli per le bar, 
                case OnState.Bar:

                    // --> Prelevo lo stato attuale del prezzo
                    CurrentStateLine myPricePosition = _checkCurrentState(lineprice);

                    if (myPricePosition == CurrentStateLine.OverBar)
                    {

                        if (directive[0] == Flag.OverBar)
                        {

                            _alert(myline, directive[4]);
                            directive[0] = Flag.DISABLED;

                        }

                        if (directive[1] == Flag.OverBar)
                        {

                            _close(myline);
                            directive[1] = Flag.DISABLED;

                        }

                        if (directive[2] == Flag.OpenBuyStopBar)
                        {

                            _open(myline, TradeType.Buy, directive[3]);
                            directive[2] = Flag.DISABLED;

                        }

                    }
                    else if (myPricePosition == CurrentStateLine.UnderBar)
                    {

                        if (directive[0] == Flag.UnderBar)
                        {

                            _alert(myline, directive[4]);
                            directive[0] = Flag.DISABLED;

                        }

                        if (directive[1] == Flag.UnderBar)
                        {

                            _close(myline);
                            directive[1] = Flag.DISABLED;

                        }

                        if (directive[2] == Flag.OpenSellStopBar)
                        {

                            _open(myline, TradeType.Sell, directive[3]);
                            directive[2] = Flag.DISABLED;

                        }

                    }
                    else if (myPricePosition == CurrentStateLine.OnGAP)
                    {

                        double gapBar = Math.Round( Math.Abs(Bars.ClosePrices.Last(1) - Bars.OpenPrices.Last(0)) / Symbol.PipSize, 2);

                        // --> Procedo se il GAP è nella norma
                        if (gapBar <= MaxGAPBar)
                        {

                            _alert(myline, directive[4]);
                            _close(myline);

                            switch (directive[2])
                            {

                                case Flag.OpenBuyStopBar:

                                    _open(myline, TradeType.Buy, directive[3]);
                                    break;

                                case Flag.OpenSellStopBar:

                                    _open(myline, TradeType.Sell, directive[3]);
                                    break;

                            }

                            _log("GAP (" + gapBar + ") then Triggered (check cBot setup)");

                        }
                        else
                        {

                            _log("GAP (" + gapBar + ") then Disabled (check cBot setup)");

                        }

                        // --> Disabilito a prescindere
                        directive[0] = Flag.DISABLED;
                        directive[1] = Flag.DISABLED;
                        directive[2] = Flag.DISABLED;

                    }

                    break;
                default:


                    // --> Prelevo lo stato attuale del prezzo
                    CurrentStateLine myPricePositionForAsk = _checkCurrentState(lineprice, Ask);
                    CurrentStateLine myPricePositionForBid = _checkCurrentState(lineprice, Bid);

                    if (myPricePositionForAsk == CurrentStateLine.Over)
                    {

                        if (directive[0] == Flag.Over)
                        {

                            _alert(myline, directive[4]);
                            directive[0] = Flag.DISABLED;

                        }
                        /*
                        if (directive[1] == Flag.Over)
                        {

                            _close(myline);
                            directive[1] = Flag.DISABLED;

                        }
                        */
                        if (directive[2] == Flag.OpenBuyStop)
                        {

                            _open(myline, TradeType.Buy, directive[3]);
                            directive[2] = Flag.DISABLED;

                        }

                    }
                    else if (myPricePositionForAsk == CurrentStateLine.Under)
                    {

                        if (directive[0] == Flag.Under)
                        {

                            _alert(myline, directive[4]);
                            directive[0] = Flag.DISABLED;

                        }

                        if (directive[1] == Flag.Under)
                        {

                            _close(myline);
                            directive[1] = Flag.DISABLED;

                        }

                        if (directive[2] == Flag.OpenBuyLimit)
                        {

                            _open(myline, TradeType.Buy, directive[3]);
                            directive[2] = Flag.DISABLED;

                        }

                    }

                    if (myPricePositionForBid == CurrentStateLine.Over)
                    {

                        if (directive[0] == Flag.Over)
                        {

                            _alert(myline, directive[4]);
                            directive[0] = Flag.DISABLED;

                        }

                        if (directive[1] == Flag.Over)
                        {

                            _close(myline);
                            directive[1] = Flag.DISABLED;

                        }

                        if (directive[2] == Flag.OpenSellLimit)
                        {

                            _open(myline, TradeType.Sell, directive[3]);
                            directive[2] = Flag.DISABLED;

                        }

                    }
                    else if (myPricePositionForBid == CurrentStateLine.Under)
                    {

                        if (directive[0] == Flag.Under)
                        {

                            _alert(myline, directive[4]);
                            directive[0] = Flag.DISABLED;

                        }
                        /*
                        if (directive[1] == Flag.Under)
                        {

                            _close(myline);
                            directive[1] = Flag.DISABLED;

                        }
                        */
                        if (directive[2] == Flag.OpenSellStop)
                        {

                            _open(myline, TradeType.Sell, directive[3]);
                            directive[2] = Flag.DISABLED;

                        }

                    }

                    break;

            }

            // --> Ricostruisco le direttive
            myline.Comment = string.Join("/", directive);

        }

        private bool _checkFeedback(ChartTrendLine myline, string[] directive)
        {

            // --> Mi aspetto 5 elementi altrimenti avanti un altro
            if (directive.Length != 5)
                return false;

            // --> L'ultima ha la precedenza, ovvero l'apertura
            if (directive[2] == Flag.OpenBuyStop)
            {

                myline.ToBuy().Stop();

            }
            else if (directive[2] == Flag.OpenBuyStopBar)
            {

                myline.ToBuy().StopBar();

            }
            else if (directive[2] == Flag.OpenBuyLimit)
            {

                myline.ToBuy().Limit();

            }
            else if (directive[2] == Flag.OpenSellStop)
            {

                myline.ToSell().Stop();

            }
            else if (directive[2] == Flag.OpenSellStopBar)
            {

                myline.ToSell().StopBar();

            }
            else if (directive[2] == Flag.OpenSellLimit)
            {

                myline.ToSell().Limit();

            }
            // --> Le chiusure
            else if (directive[1] == Flag.Over || directive[1] == Flag.Under)
            {

                myline.ToClose();

            }
            else if (directive[1] == Flag.OverBar || directive[1] == Flag.UnderBar)
            {

                myline.ToCloseBar();

            }
            // --> Gli alerts
            else if (directive[0] == Flag.Over || directive[0] == Flag.Under)
            {

                myline.ToAlert();

            }
            else if (directive[0] == Flag.OverBar || directive[0] == Flag.UnderBar)
            {

                myline.ToAlertBar();

            }
            else if (directive[0] == Flag.DISABLED && directive[1] == Flag.DISABLED && directive[2] == Flag.DISABLED)
            {

                myline.ToAllDisabled();

            }
            else
            {

                return false;

            }

            return true;

        }

        private void _alert(ChartTrendLine myline = null, string custom = null)
        {

            if (!CanDraw)
                return;

            string tmpMex = custom != null && custom.Length > 0 ? custom : "{0} : {1} breakout, Ask {2} / Bid {3}";

            string mex = string.Format(tmpMex, NAME, SymbolName, string.Format("{0:N" + Symbol.Digits + "}", Ask), string.Format("{0:N" + Symbol.Digits + "}", Bid));
            
            if (RunningMode == RunningMode.VisualBacktesting)
            {

                _log(mex);

            }
            else
            {

                // --> La popup non deve interrompere la logica delle API, apertura e chiusura
                new Thread(new ThreadStart(delegate { MessageBox.Show(mex, "BreakOut", MessageBoxButtons.OK, MessageBoxIcon.Information); })).Start();
                _toWebHook(mex);

            }

            // --> if (myline != null) myline.ToDelivered();

        }

        private void _close(ChartTrendLine myline)
        {

            // --> Chiudo tutti i trade di questo simbolo
            foreach (var position in Positions)
            {

                if (position.SymbolName != SymbolName)
                    continue;

                ClosePositionAsync(position);

            }

            // --> myline.ToDelivered();

        }

        private void _open(ChartTrendLine myline, TradeType mytype, string directive = "0,01", double slippage = 20)
        {

            double myLots = 0.01;

            try
            {

                // --> double con la virgola e non con il punto
                if (directive.IndexOf('.') == -1)
                {
                    NumberFormatInfo provider = new NumberFormatInfo();
                    provider.NumberDecimalSeparator = ",";
                    provider.NumberGroupSeparator = ".";
                    provider.NumberGroupSizes = new int[] 
                    {
                        3
                    };

                    myLots = Convert.ToDouble(directive, provider);

                }

            } catch
            {
            }

            var volumeInUnits = Symbol.QuantityToVolumeInUnits(myLots);

            TradeResult result = ExecuteMarketRangeOrder(mytype, Symbol.Name, volumeInUnits, slippage, mytype == TradeType.Buy ? Ask : Bid, MyLabel, 0, 0);

            if (!result.IsSuccessful)
                _log("can't open new trade " + mytype.ToString("G") + " (" + result.Error + ")");

            // --> Anche se non dovesse aprire la disabilito, potrebbe creare più problemi che altro
            // --> myline.ToDelivered();

        }

        private CurrentStateLine _checkCurrentState(double lineprice, double whatPrice = 0)
        {

            // --> Controllo solo le barre
            if (whatPrice == 0)
            {

                // --> Primo e secondo controllo per le bar, quindi per la precedente perchè si presume che venga chiamato OnBar
                if (Bars.OpenPrices.Last(1) < lineprice && Bars.ClosePrices.Last(1) > lineprice)
                {

                    return CurrentStateLine.OverBar;

                }
                else if (Bars.OpenPrices.Last(1) > lineprice && Bars.ClosePrices.Last(1) < lineprice)
                {

                    return CurrentStateLine.UnderBar;

                }
                else if ((Bars.ClosePrices.Last(1) < lineprice && Bars.OpenPrices.Last(0) > lineprice) || (Bars.ClosePrices.Last(1) > lineprice && Bars.OpenPrices.Last(0) < lineprice))
                {

                    return CurrentStateLine.OnGAP;

                }

            }
            else
            {

                if (whatPrice > lineprice)
                {

                    return CurrentStateLine.Over;

                }
                else if (whatPrice < lineprice)
                {

                    return CurrentStateLine.Under;

                }

            }

            return CurrentStateLine.Undefined;

        }

        private void _log(string mex)
        {

            if (!CanDraw)
                return;

            Print("{0} : {1}", NAME, mex);

        }

        private void _areaAdded(IndicatorAreaAddedEventArgs obj)
        {

            // --> Aggiorno tutte le aree
            _updateAllAreaEvents();

        }

        private void _updateAllAreaEvents()
        {

            // --> Prima rimuovo eventuali handle registrati
            try
            {

                Chart.MouseDown -= _chart_MouseDown;
                Chart.MouseUp -= _chart_MouseUp;
                Chart.ObjectSelectionChanged -= _objectSelected;

                /*
                foreach (var item in Chart.IndicatorAreas)
                {

                    item.MouseDown -= _chart_MouseDown;
                    item.MouseUp -= _chart_MouseUp;
                    item.ObjectSelectionChanged -= _objectSelected;


                }*/

            }             catch (Exception exp)
            {

                Print(exp.Message);

            }

            // --> Poi aggiungo gli handle che mi interessano
            try
            {

                Chart.MouseDown += _chart_MouseDown;
                Chart.MouseUp += _chart_MouseUp;
                Chart.ObjectSelectionChanged += _objectSelected;

                /*                
                foreach (var item in Chart.IndicatorAreas)
                {

                    item.MouseDown += _chart_MouseDown;
                    item.MouseUp += _chart_MouseUp;
                    item.ObjectSelectionChanged += _objectSelected;


                }*/

            }             catch (Exception exp)
            {

                Print(exp.Message);

            }

        }

        private void _chart_MouseDown(ChartMouseEventArgs obj)
        {

            _closeFormTrendLine();

        }

        private void _chart_MouseUp(ChartMouseEventArgs obj)
        {

            if (obj.CtrlKey)
            {

                if (TrendLineSelected == null)
                {

                    Print("Please select one trendline first");

                }
                else
                {

                    _closeFormTrendLine();

                    if (TrendLineSelected != null)
                    {

                        ThreadForm = new Thread(_createFormTrendLineOptions);

                        ThreadForm.SetApartmentState(ApartmentState.STA);
                        ThreadForm.Start(TrendLineSelected);

                    }

                }

            }

        }

        private void _objectSelected(ChartObjectSelectionChangedEventArgs obj)
        {

            TrendLineSelected = (obj.IsObjectSelected && obj.ChartObject.ObjectType == ChartObjectType.TrendLine) ? obj.ChartObject : null;

        }

        private void _createFormTrendLineOptions(object data)
        {
            try
            {

                ChartTrendLine mytrendline = (ChartTrendLine)data;

                FormTrendLineOptions = new FrmWrapper(mytrendline) 
                {

                    Icon = Icons.logo

                };

                FormTrendLineOptions.GoToMyPage += delegate { System.Diagnostics.Process.Start(PAGE); };
                // -->(object sender, FrmWrapper.TrendLineData args)

                                /*
ChartTrendLine tmp = args.TrendLine;

ChartTrendLine newTrendLine = Chart.DrawTrendLine(tmp.Name, tmp.Time1, tmp.Y1, tmp.Time2, tmp.Y2, tmp.Color);                    
newTrendLine.Comment = tmp.Comment;

Chart.DrawStaticText("sssss", "saved " + args.TrendLine.Comment, VerticalAlignment.Center, API.HorizontalAlignment.Center, Color.Red);
*/

FormTrendLineOptions.UpdateTrendLine += delegate
                {

                    // --> Chiudo la finestra
                    FormTrendLineOptions.Close();

                    // --> Aggiorno le trendlines
                    _checkTrendLines(OnState.Tick);

                };

                FormTrendLineOptions.ShowDialog();

            } catch (Exception exp)
            {

                Print(exp.Message);

            }

        }

        private void _closeFormTrendLine()
        {

            try
            {

                FormTrendLineOptions.Close();

            } catch
            {

            }

        }

        private void _delegateChartAdded(ChartObjectsAddedEventArgs objs)
        {

            foreach ( ChartObject obj in objs.ChartObjects) {

                if (obj.ObjectType == ChartObjectType.TrendLine)
                {

                    ChartTrendLine mytrendline = (ChartTrendLine)obj;

                    if (mytrendline.Comment == null || mytrendline.Comment.Trim().Length < 1)
                    {

                        mytrendline.Thickness = 1;
                        mytrendline.LineStyle = LineStyle.Dots;
                        mytrendline.Color = Color.Gray;

                        TrendLineSelected = mytrendline;

                    }

                }

            }

        }
        
        public void _toWebHook(string custom = null)
        {

            if (!WebhookEnabled)
                return;

            string tmpMex = custom != null && custom.Length > 0 ? custom : "{0} : {1} breakout, Ask {2} / Bid {3}";

            string messageformat = string.Format(tmpMex, NAME, SymbolName, string.Format("{0:N" + Symbol.Digits + "}", Ask), string.Format("{0:N" + Symbol.Digits + "}", Bid));

            try
            {
                // --> Mi servono i permessi di sicurezza per il dominio, compreso i redirect
                Uri myuri = new Uri(Webhook);

                string pattern = string.Format("{0}://{1}/.*", myuri.Scheme, myuri.Host);

                // --> Autorizzo tutte le pagine di questo dominio
                Regex urlRegEx = new Regex(pattern);
                WebPermission p = new WebPermission(NetworkAccess.Connect, urlRegEx);
                p.Assert();

                // --> Protocollo di sicurezza https://
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;

                using (WebClient wc = new WebClient())
                {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    string HtmlResult = wc.UploadString(myuri, string.Format(PostParams, messageformat));
                }

            } catch (Exception exc)
            {

                MessageBox.Show(string.Format("{0}\r\nstopping cBots 'TrendLine Power' ...", exc.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Stop();

            }

        }

        #endregion

    }

}
