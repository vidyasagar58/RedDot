﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RedDot
{
    /// <summary>
    /// Interaction logic for DeleteConfirm.xaml
    /// </summary>
    public partial class Confirm : Window
    {
        public string Action = "";
        public string Response = "";
        public static string OK = "Yes";
  
        DispatcherTimer dispatchTimer = new DispatcherTimer();
        int counter = 0;
        public Confirm(string message = "",int timeout=0)
        {
            counter = timeout;
            InitializeComponent();
            if (message != "") tbMessage.Text = message;

            dispatchTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatchTimer.Interval = new TimeSpan(0, 0, 1);
            this.tbCounter.Text = "";

            if (timeout > 0)
                dispatchTimer.Start();

        }


        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            counter--;
            this.tbCounter.Text = counter.ToString();
            if(counter <=0)
            {
                dispatchTimer.Stop();
                Action = "Yes";
                Response = Action;
                this.Close();
            }
        

        }
        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            Button chosen = sender as Button;
            Action = chosen.Tag.ToString().Trim();
            Response = chosen.Tag.ToString().Trim();
            this.Close();
        }

        public static bool Ask(string message, int timeout=0)
        {
            Confirm conf = new Confirm(message,timeout)
            {
                Topmost = true,
                ShowInTaskbar = false
            };
            conf.ShowDialog();
            if (conf.Response == Confirm.OK) return true; else return false;

        }
    }
}
