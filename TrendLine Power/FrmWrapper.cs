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

        public event EventHandler GoToMyPage;

        private ChartTrendLine MyTrendLine = null;

        #endregion

        public void GoExternToPage()
        {
            GoToMyPage.Invoke(null, EventArgs.Empty);
        }

        public void Save(string comment)
        {

            MyTrendLine.Comment = comment;
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
