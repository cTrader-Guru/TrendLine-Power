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
            Undefined

        }

        public enum OnState
        {

            Tick,
            Bar

        }

        #endregion

        #region Identity

        public const string NAME = "Trendline Power";

        public const string VERSION = "2.0.3";

        public const string PAGE = "https://ctrader.guru/product/trendline-power/";

        #endregion

        #region Params

        #endregion

        #region Property

        bool CanDraw = false;
        Thread ThreadForm;
        FrmWrapper FormTrendLineOptions;
        bool KeyDownCTRL = false;

        #endregion

        #region cBot Events

        protected override void OnStart()
        {

            // --> Con questo evitiamo errori comuni in backtest
            CanDraw = RunningMode == RunningMode.RealTime || RunningMode == RunningMode.VisualBacktesting;

            // --> Stampo nei log la versione corrente
            _log(string.Format("{0} {1}", VERSION, PAGE));

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

        private void _checkTrendLines(OnState mystate)
        {

            // --> Prelevo le trendline
            ChartTrendLine[] alltrendlines = Chart.FindAllObjects<ChartTrendLine>();

            // --> Le passo al setaccio
            foreach (ChartTrendLine myline in alltrendlines)
            {

                // --> Se non è inizializzata non devo fare nulla
                if (myline.Comment == null)
                    continue;

                // --> Potrebbe essere in un protoccollo non in linea con le aspettative
                string[] directive = myline.Comment.Split(Flag.Separator);

                // --> Aggiorno il feedback visivo
                if (!_checkFeedback(myline, directive))
                    continue;

                // --> Se la trendline non è infinita allora devo controllare il tempo
                if (!myline.ExtendToInfinity && myline.Time1 < Bars.LastBar.OpenTime && myline.Time2 < Bars.LastBar.OpenTime)
                {

                    myline.ToDelivered();
                    continue;

                }

                // --> Prelevo il prezzo della trendline
                double lineprice = Math.Round(myline.CalculateY(Chart.BarsTotal - 1), Symbol.Digits);

                // --> Prelevo lo stato attuale del prezzo
                CurrentStateLine myPricePosition = _checkCurrentState(lineprice);

                switch (mystate)
                {

                    // --> Solo controlli per le bar, 
                    case OnState.Bar:

                        if (myPricePosition == CurrentStateLine.OverBar)
                        {

                            if (directive[0] == Flag.OverBar)
                                _alert(myline);

                            if (directive[1] == Flag.OverBar)
                                _close(myline);

                            if (directive[2] == Flag.OpenBuyStopBar)
                                _open(myline, TradeType.Buy, directive[3]);

                        }
                        else if (myPricePosition == CurrentStateLine.UnderBar)
                        {

                            if (directive[0] == Flag.UnderBar)
                                _alert(myline);

                            if (directive[1] == Flag.UnderBar)
                                _close(myline);

                            if (directive[2] == Flag.OpenSellStopBar)
                                _open(myline, TradeType.Sell, directive[3]);

                        }

                        break;
                    default:


                        if (myPricePosition == CurrentStateLine.Over)
                        {

                            if (directive[0] == Flag.Over)
                                _alert(myline);

                            if (directive[1] == Flag.Over)
                                _close(myline);

                            if (directive[2] == Flag.OpenBuyStop)
                            {

                                _open(myline, TradeType.Buy, directive[3]);

                            }
                            else if (directive[2] == Flag.OpenSellLimit)
                            {

                                _open(myline, TradeType.Sell, directive[3]);

                            }

                        }
                        else if (myPricePosition == CurrentStateLine.Under)
                        {

                            if (directive[0] == Flag.Under)
                                _alert(myline);

                            if (directive[1] == Flag.Under)
                                _close(myline);

                            if (directive[2] == Flag.OpenSellStop)
                            {

                                _open(myline, TradeType.Sell, directive[3]);

                            }
                            else if (directive[2] == Flag.OpenBuyLimit)
                            {

                                _open(myline, TradeType.Buy, directive[3]);

                            }

                        }

                        break;

                }

            }

        }

        private bool _checkFeedback(ChartTrendLine myline, string[] directive)
        {

            // --> Mi aspetto 4 elementi altrimenti avanti un altro
            if (directive.Length != 4)
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

        private void _alert(ChartTrendLine myline)
        {

            if (!CanDraw)
                return;

            string mex = string.Format("{0} : {1} breakout, Ask {2} / Bid {3}", NAME, SymbolName, Ask.ToString(), Bid.ToString());

            if (RunningMode == RunningMode.VisualBacktesting)
            {

                _log(mex);

            }
            else
            {

                // --> La popup non deve interrompere la logica delle API, apertura e chiusura
                new Thread(new ThreadStart(delegate { MessageBox.Show(mex, "BreakOut", MessageBoxButtons.OK, MessageBoxIcon.Information); })).Start();

            }

            myline.ToDelivered();

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

            myline.ToDelivered();

        }

        private void _open(ChartTrendLine myline, TradeType mytype, string directive = "0,01", string mylabel = null, double slippage = 20)
        {

            double myLots = 0.01;

            try
            {

                // --> double con la virgola e non con il punto
                if (directive.IndexOf('.') == -1)
                    myLots = Convert.ToDouble(directive);

            } catch
            {
            }

            var volumeInUnits = Symbol.QuantityToVolumeInUnits(myLots);

            TradeResult result = ExecuteMarketRangeOrder(mytype, Symbol.Name, volumeInUnits, slippage, mytype == TradeType.Buy ? Ask : Bid, mylabel, 0, 0);

            if (!result.IsSuccessful)
                _log("can't open new trade " + mytype.ToString("G") + " (" + result.Error + ")");

            // --> Anche se non dovesse aprire la disabilito, potrebbe creare più problemi che altro
            myline.ToDelivered();

        }

        private CurrentStateLine _checkCurrentState(double lineprice)
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
            else if (Bid > lineprice)
            {

                return CurrentStateLine.Over;

            }
            else if (Ask < lineprice)
            {

                return CurrentStateLine.Under;

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

                Chart.ObjectSelectionChanged -= _objectSelected;
                Chart.MouseMove -= _onMouseMove;

                foreach (var item in Chart.IndicatorAreas)
                {

                    item.ObjectSelectionChanged -= _objectSelected;
                    item.MouseMove -= _onMouseMove;


                }

            } catch (Exception exp)
            {

                Print(exp.Message);

            }

            // --> Poi aggiungo gli handle che mi interessano
            try
            {

                Chart.ObjectSelectionChanged += _objectSelected;
                Chart.MouseMove += _onMouseMove;

                foreach (var item in Chart.IndicatorAreas)
                {

                    item.ObjectSelectionChanged += _objectSelected;
                    item.MouseMove += _onMouseMove;


                }

            } catch (Exception exp)
            {

                Print(exp.Message);

            }

        }

        private void _objectSelected(ChartObjectSelectionChangedEventArgs obj)
        {

            if (obj.ChartObject.ObjectType == ChartObjectType.TrendLine)
            {

                _closeFormTrendLine();

                if (obj.IsObjectSelected && KeyDownCTRL)
                {

                    ThreadForm = new Thread(_createFormTrendLineOptions);

                    ThreadForm.SetApartmentState(ApartmentState.STA);
                    ThreadForm.Start(obj.ChartObject);

                }

            }

        }

        private void _createFormTrendLineOptions(object data)
        {
            try
            {

                FormTrendLineOptions = new FrmWrapper((ChartTrendLine)data) 
                {

                    Icon = Icons.logo

                };
                               
                FormTrendLineOptions.GoToMyPage += delegate { System.Diagnostics.Process.Start(PAGE); };

                // --> FormTrendLineOptions.FormClosed += delegate { /*TODO*/ };

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

        private void _onMouseMove(ChartMouseEventArgs eventArgs)
        {

            KeyDownCTRL = eventArgs.CtrlKey;

        }


        #endregion

    }

}
