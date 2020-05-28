using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using cAlgo.API;

namespace cAlgo
{
    [ComVisible(true)]
    public partial class FrmWrapper: Form
    {
        #region Property

        public class TrendLineData : EventArgs
        {

            public ChartTrendLine TrendLine { get; set; }

        }

        public event EventHandler GoToMyPage;
        public event EventHandler UpdateTrendLine;
        // --> public event EventHandler<TrendLineData> UpdateTrendLine;

        private ChartTrendLine MyTrendLine = null;

        #endregion

        public void GoExternToPage()
        {
            GoToMyPage.Invoke(null, EventArgs.Empty);
        }

        public void Save(string comment)
        {

            MyTrendLine.Comment = comment;

            /* --> A prove fatte comunque dobbiamo aspettare il rendering del Chart (OnTick)
             * 
            TrendLineData args = new TrendLineData();
            args.TrendLine = MyTrendLine;

            UpdateTrendLine.Invoke(null, args);
            */
            
            UpdateTrendLine.Invoke(null, EventArgs.Empty);
            Close();

        }

        public string GetTrendComment()
        {

            return MyTrendLine.Comment;

        }

        public FrmWrapper(ChartTrendLine trendline)
        {

            MyTrendLine = trendline;

            InitializeComponent();

        }

        private void FrmWrapper_Load(object sender, EventArgs e)
        {

            // --> Autorizzo gli script
            mybrowser.ObjectForScripting = this;

            // --> Carico la pagina iniziale
            mybrowser.DocumentText = HTML.index;
                        
        }

    }

}
