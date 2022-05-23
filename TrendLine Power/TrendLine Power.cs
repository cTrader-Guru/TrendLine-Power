/*  CTRADER GURU --> Template 1.0.6

    Homepage    : https://ctrader.guru/
    Telegram    : https://t.me/ctraderguru
    Twitter     : https://twitter.com/cTraderGURU/
    Facebook    : https://www.facebook.com/ctrader.guru/
    YouTube     : https://www.youtube.com/channel/UCKkgbw09Fifj65W5t5lHeCQ
    GitHub      : https://github.com/cTraderGURU/
    TOS         : https://github.com/cTrader-Guru/ctrader-guru.github.io/blob/main/DISCLAIMER.md

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

        public const string VERSION = "2.1.7";

        public const string PAGE = "https://www.google.com/search?q=ctrader+guru+trendline+power";

        #endregion

        #region Params

        [Parameter(NAME + " " + VERSION, Group = "Identity", DefaultValue = PAGE)]
        public string ProductInfo { get; set; }

        [Parameter("Label ( Magic Name )", Group = "Identity", DefaultValue = NAME)]
        public string MyLabel { get; set; }

        [Parameter("Max GAP between Bars (pips, over = disable) :", Group = "Strategy", DefaultValue = 3)]
        public double MaxGAPBar { get; set; }

        [Parameter("Key", DefaultValue = Key.T, Group = "Hotkey")]
        public Key Hotkey { get; set; }

        [Parameter("Modifier Key", DefaultValue = ModifierKeys.Shift, Group = "Hotkey")]
        public ModifierKeys HotkeyModifierKey { get; set; }

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

            CanDraw = RunningMode == RunningMode.RealTime || RunningMode == RunningMode.VisualBacktesting;

            Log(string.Format("{0} {1}", VERSION, PAGE));

            Log(string.Format("Please select one trendline on main chart then press {0} + {1}", HotkeyModifierKey, Hotkey));

            Chart.ObjectsAdded += DelegateChartAdded;

            UpdateAllAreaEvents();

            Chart.AddHotkey(OnHotkey, Hotkey, HotkeyModifierKey);

        }

        protected override void OnStop()
        {

            CloseFormTrendLine();

        }

        protected override void OnTick()
        {

            CheckTrendLines(OnState.Tick);

        }

        protected override void OnBar()
        {

            CheckTrendLines(OnState.Bar);

        }

        #endregion

        #region Private Methods

        private void CheckTrendLines(OnState mystate, ChartTrendLine OneLine = null)
        {

            if (OneLine != null)
            {

                ManageTrendLine(mystate, OneLine);
                return;

            }

            ChartTrendLine[] alltrendlines = Chart.FindAllObjects<ChartTrendLine>();

            foreach (ChartTrendLine myline in alltrendlines)
            {

                ManageTrendLine(mystate, myline);

            }

        }

        private void ManageTrendLine(OnState mystate, ChartTrendLine myline)
        {


            if (myline.Comment == null)
                return;

            string[] directive = myline.Comment.Split(Flag.Separator);

            if (!CheckFeedback(myline, directive))
                return;

            if (!myline.ExtendToInfinity && myline.Time1 < Bars.LastBar.OpenTime && myline.Time2 < Bars.LastBar.OpenTime)
            {

                myline.ToDelivered();
                return;

            }
            else if (myline.Time1 > Bars.LastBar.OpenTime && myline.Time2 > Bars.LastBar.OpenTime)
            {

                return;

            }

            double lineprice = Math.Round(myline.CalculateY(Chart.BarsTotal - 1), Symbol.Digits);

            switch (mystate)
            {

                case OnState.Bar:

                    CurrentStateLine myPricePosition = CheckCurrentState(lineprice);

                    if (myPricePosition == CurrentStateLine.OverBar)
                    {

                        if (directive[0] == Flag.OverBar)
                        {

                            Alert(directive[4]);
                            directive[0] = Flag.DISABLED;

                        }

                        if (directive[1] == Flag.OverBar)
                        {

                            Close();
                            directive[1] = Flag.DISABLED;

                        }

                        if (directive[2] == Flag.OpenBuyStopBar)
                        {

                            Open(TradeType.Buy, directive[3]);
                            directive[2] = Flag.DISABLED;

                        }

                    }
                    else if (myPricePosition == CurrentStateLine.UnderBar)
                    {

                        if (directive[0] == Flag.UnderBar)
                        {

                            Alert(directive[4]);
                            directive[0] = Flag.DISABLED;

                        }

                        if (directive[1] == Flag.UnderBar)
                        {

                            Close();
                            directive[1] = Flag.DISABLED;

                        }

                        if (directive[2] == Flag.OpenSellStopBar)
                        {

                            Open(TradeType.Sell, directive[3]);
                            directive[2] = Flag.DISABLED;

                        }

                    }
                    else if (myPricePosition == CurrentStateLine.OnGAP)
                    {

                        double gapBar = Math.Round(Math.Abs(Bars.ClosePrices.Last(1) - Bars.OpenPrices.Last(0)) / Symbol.PipSize, 2);

                        if (gapBar <= MaxGAPBar)
                        {

                            Alert(directive[4]);
                            Close();

                            switch (directive[2])
                            {

                                case Flag.OpenBuyStopBar:

                                    Open(TradeType.Buy, directive[3]);
                                    break;

                                case Flag.OpenSellStopBar:

                                    Open(TradeType.Sell, directive[3]);
                                    break;

                            }

                            Log("GAP (" + gapBar + ") then Triggered (check cBot setup)");

                        }
                        else
                        {

                            Log("GAP (" + gapBar + ") then Disabled (check cBot setup)");

                        }

                        directive[0] = Flag.DISABLED;
                        directive[1] = Flag.DISABLED;
                        directive[2] = Flag.DISABLED;

                    }

                    break;
                default:


                    CurrentStateLine myPricePositionForAsk = CheckCurrentState(lineprice, Ask);
                    CurrentStateLine myPricePositionForBid = CheckCurrentState(lineprice, Bid);

                    if (myPricePositionForAsk == CurrentStateLine.Over)
                    {

                        if (directive[0] == Flag.Over)
                        {

                            Alert(directive[4]);
                            directive[0] = Flag.DISABLED;

                        }

                        if (directive[2] == Flag.OpenBuyStop)
                        {

                            Open(TradeType.Buy, directive[3]);
                            directive[2] = Flag.DISABLED;

                        }

                    }
                    else if (myPricePositionForAsk == CurrentStateLine.Under)
                    {

                        if (directive[0] == Flag.Under)
                        {

                            Alert(directive[4]);
                            directive[0] = Flag.DISABLED;

                        }

                        if (directive[1] == Flag.Under)
                        {

                            Close();
                            directive[1] = Flag.DISABLED;

                        }

                        if (directive[2] == Flag.OpenBuyLimit)
                        {

                            Open(TradeType.Buy, directive[3]);
                            directive[2] = Flag.DISABLED;

                        }

                    }

                    if (myPricePositionForBid == CurrentStateLine.Over)
                    {

                        if (directive[0] == Flag.Over)
                        {

                            Alert(directive[4]);
                            directive[0] = Flag.DISABLED;

                        }

                        if (directive[1] == Flag.Over)
                        {

                            Close();
                            directive[1] = Flag.DISABLED;

                        }

                        if (directive[2] == Flag.OpenSellLimit)
                        {

                            Open(TradeType.Sell, directive[3]);
                            directive[2] = Flag.DISABLED;

                        }

                    }
                    else if (myPricePositionForBid == CurrentStateLine.Under)
                    {

                        if (directive[0] == Flag.Under)
                        {

                            Alert(directive[4]);
                            directive[0] = Flag.DISABLED;

                        }

                        if (directive[2] == Flag.OpenSellStop)
                        {

                            Open(TradeType.Sell, directive[3]);
                            directive[2] = Flag.DISABLED;

                        }

                    }

                    break;

            }

            myline.Comment = string.Join("/", directive);

        }

        private bool CheckFeedback(ChartTrendLine myline, string[] directive)
        {

            if (directive.Length != 5)
                return false;

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
            else if (directive[1] == Flag.Over || directive[1] == Flag.Under)
            {

                myline.ToClose();

            }
            else if (directive[1] == Flag.OverBar || directive[1] == Flag.UnderBar)
            {

                myline.ToCloseBar();

            }
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

        private void Alert(string custom = null)
        {

            if (!CanDraw)
                return;

            string tmpMex = custom != null && custom.Length > 0 ? custom : "{0} : {1} breakout, Ask {2} / Bid {3}";

            string mex = string.Format(tmpMex, NAME, SymbolName, string.Format("{0:N" + Symbol.Digits + "}", Ask), string.Format("{0:N" + Symbol.Digits + "}", Bid));

            if (RunningMode == RunningMode.VisualBacktesting)
            {

                Log(mex);

            }
            else
            {

                new Thread(new ThreadStart(delegate { MessageBox.Show(mex, "BreakOut", MessageBoxButtons.OK, MessageBoxIcon.Information); })).Start();
                ToWebHook(mex);

            }

        }

        private void Close()
        {

            foreach (var position in Positions)
            {

                if (position.SymbolName != SymbolName)
                    continue;

                ClosePositionAsync(position);

            }

        }

        private void Open(TradeType mytype, string directive = "0,01", double slippage = 20)
        {

            double myLots = 0.01;

            try
            {

                if (directive.IndexOf('.') == -1)
                {

                    NumberFormatInfo provider = new NumberFormatInfo 
                    {
                        NumberDecimalSeparator = ",",
                        NumberGroupSeparator = ".",
                        NumberGroupSizes = new int[] 
                        {
                            3
                        }
                    };

                    myLots = Convert.ToDouble(directive, provider);

                }

            } catch
            {
            }

            var volumeInUnits = Symbol.QuantityToVolumeInUnits(myLots);

            TradeResult result = ExecuteMarketRangeOrder(mytype, Symbol.Name, volumeInUnits, slippage, mytype == TradeType.Buy ? Ask : Bid, MyLabel, 0, 0);

            if (!result.IsSuccessful)
                Log("can't open new trade " + mytype.ToString("G") + " (" + result.Error + ")");


        }

        private CurrentStateLine CheckCurrentState(double lineprice, double whatPrice = 0)
        {

            if (whatPrice == 0)
            {

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

        private void Log(string mex, bool box = false)
        {

            if (!CanDraw)
                return;

            Print("{0} : {1}", NAME, mex);

            if (box)
                MessageBox.Show(mex, NAME, MessageBoxButtons.OK, MessageBoxIcon.Information);

        }


        private void UpdateAllAreaEvents()
        {

            try
            {

                Chart.ObjectsSelectionChanged -= ObjectSelected;

            } catch (Exception exp)
            {

                Print(exp.Message);

            }

            try
            {

                Chart.ObjectsSelectionChanged += ObjectSelected;

            } catch (Exception exp)
            {

                Print(exp.Message);

            }

        }

        private void OnHotkey()
        {

            if (TrendLineSelected == null)
            {

                Log("Please select one trendline on main chart", true);

            }
            else
            {

                CloseFormTrendLine();

                ThreadForm = new Thread(CreateFormTrendLineOptions);

                ThreadForm.SetApartmentState(ApartmentState.STA);
                ThreadForm.Start(TrendLineSelected);

            }


        }

        private void ObjectSelected(ChartObjectsSelectionChangedEventArgs obj)
        {

            if (Chart.SelectedObjects.Count != 1)
            {

                TrendLineSelected = null;
                return;

            }

            TrendLineSelected = (Chart.SelectedObjects[0].ObjectType == ChartObjectType.TrendLine) ? Chart.SelectedObjects[0] : null;

        }

        private void CreateFormTrendLineOptions(object data)
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
                    CheckTrendLines(OnState.Tick);

                };

                FormTrendLineOptions.ShowDialog();

            } catch (Exception exp)
            {

                Print(exp.Message);

            }

        }

        private void CloseFormTrendLine()
        {

            try
            {

                FormTrendLineOptions.Close();

            } catch
            {

            }

        }

        private void DelegateChartAdded(ChartObjectsAddedEventArgs objs)
        {

            foreach (ChartObject obj in objs.ChartObjects)
            {

                if (obj.ObjectType == ChartObjectType.TrendLine)
                {

                    ChartTrendLine mytrendline = (ChartTrendLine)obj;

                    if (mytrendline.Comment == null || mytrendline.Comment.Trim().Length < 1)
                    {

                        mytrendline.Thickness = 1;
                        mytrendline.LineStyle = LineStyle.Dots;
                        mytrendline.Color = Color.Gray;

                    }

                }

            }

        }

        public void ToWebHook(string custom = null)
        {

            if (!WebhookEnabled)
                return;

            string tmpMex = custom != null && custom.Length > 0 ? custom : "{0} : {1} breakout, Ask {2} / Bid {3}";

            string messageformat = string.Format(tmpMex, NAME, SymbolName, string.Format("{0:N" + Symbol.Digits + "}", Ask), string.Format("{0:N" + Symbol.Digits + "}", Bid));

            try
            {

                Uri myuri = new Uri(Webhook);

                string pattern = string.Format("{0}://{1}/.*", myuri.Scheme, myuri.Host);

                Regex urlRegEx = new Regex(pattern);
                WebPermission p = new WebPermission(NetworkAccess.Connect, urlRegEx);
                p.Assert();

                ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;

                using (WebClient wc = new WebClient())
                {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    string HtmlResult = wc.UploadString(myuri, string.Format(PostParams, messageformat));
                }

            } catch (Exception exc)
            {

                Log(string.Format("{0}\r\nstopping cBots 'TrendLine Power' ...", exc.Message), true);
                Stop();

            }

        }

        #endregion

    }

}
