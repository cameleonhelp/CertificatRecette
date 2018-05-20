using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace CertificatRecette
{
    public partial class SplashForm : Form
    {
        public int Progress
        {
            get
            {
                return this.progressBar1.Value;
            }
            set
            {
                this.progressBar1.Value = value;
            }
        }

        private delegate void ProgressDelegate(int progress);

        private ProgressDelegate del;


        private void UpdateProgressInternal(int progress)
        {
            if (this.Handle == null)
            {
                return;
            }

            this.progressBar1.Value = progress;
        }

        public void UpdateProgress(int progress)
        {
            this.Invoke(del, progress);
        }

        public SplashForm()
        {
            InitializeComponent();
            this.progressBar1.Maximum = 100;
            del = this.UpdateProgressInternal;
        }

        private void SplashForm_Load(object sender, EventArgs e)
        {
            var productInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            label3.Text = Application.ProductVersion + " " + productInfo.LegalCopyright;
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }

    public class Hardworker
    {
        public event EventHandler<HardWorkerEventArgs> ProgressChanged;
        public event EventHandler HardWorkDone;

        public void DoHardWork()
        {
            for (int i = 1; i <= 100; i++)
            {
                for (int j = 1; j <= 500000; j++)
                {
                    Math.Pow(i, j);
                }
                this.OnProgressChanged(i);
            }

            this.OnHardWorkDone();
        }

        private void OnProgressChanged(int progress)
        {
            var handler = this.ProgressChanged;
            if (handler != null)
            {
                handler(this, new HardWorkerEventArgs(progress));
            }
        }

        private void OnHardWorkDone()
        {
            var handler = this.HardWorkDone;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }

    public class HardWorkerEventArgs : EventArgs
    {
        public HardWorkerEventArgs(int progress)
        {
            this.Progress = progress;
        }

        public int Progress
        {
            get;
            private set;
        }
    }
}
