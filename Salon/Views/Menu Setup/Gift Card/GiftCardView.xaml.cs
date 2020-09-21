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
    /// Interaction logic for GiftCardView.xaml
    /// </summary>
    public partial class GiftCardView : Window
    {
        private PaymentViewModel paymentviewmodel;
        public string RawString = "";
        public string CardNumber = "";
        private string m_cardnumber = "";
        int trackcount = 0;

        HeartPOS m_ccp;
        public GiftCardView(Ticket m_ticket, HeartPOS ccp)
        {
            InitializeComponent();

            paymentviewmodel = new PaymentViewModel(this, m_ticket,ccp,"","");
            this.DataContext = paymentviewmodel;

            if (GlobalSettings.Instance.CreditCardProcessor == "HeartSIP")
                if (ccp != null)
                {
                    m_ccp = ccp;
                    int requestid = Utility.RandomPin(8);
                    ccp.ExecuteGetCardData(requestid.ToString());
                    ccp.getdataResponse = Response_GetCardData;
                }
                else
                {
                    TouchMessageBox.Show("Credit Card Processor Object is null!!!");
                }

        }

        private void Response_GetCardData(string requestid, string trackdata)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                Process_Response_GetData(requestid, trackdata);
            }));
        }


        private void Process_Response_GetData(string requestid, string trackdata)
        {
            if(trackdata != "")
            {
                m_cardnumber = trackdata.Replace("B", "").Replace("R", "").Replace("?", "").Replace("\r", "");
                tbGiftCard.Text = m_cardnumber;

                decimal balance;
                balance = paymentviewmodel.CheckBalance(m_cardnumber);

                //verify if card has been activated (0 or greater)
                if (balance == -99)
                {
                    tbMessage.Foreground = Brushes.Red;
                    tbMessage.Text = "Gift Card NOT Activated";
                    // RawString = "";
                }
         
            }
       
        }


        private void CancelClick(object sender, RoutedEventArgs e)
        {
            if (GlobalSettings.Instance.CreditCardProcessor == "HeartSIP")
                if (m_ccp != null) m_ccp.ExecuteCommand("Reset");
                 
               
            this.Close();
        }


        private void Window_TextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == "%")
            {
                this.tbMessage.Foreground = Brushes.White;
                this.tbMessage.Text = "";
                tbGiftCard.Text = "Reading ...";
                RawString = "";
                trackcount = 0;
            }
            RawString = RawString + e.Text;

            if (e.Text == "?")
            {
                trackcount++;

            }
            if (e.Text == "\r") ProcessInput(RawString);


            this.tbTemp.Text = RawString;

        }

        private void ProcessInput(string inputstr)
        {

          
            try
            {
                if (trackcount >= 1)
                {
                    string[] tracks = RawString.Split(';');

                    string[] data = tracks[0].Split('^');

                    string data1 = data[0].ToUpper();
                   // this.tbTemp.Text = data1;
                    decimal balance;
                   // RawString = "";

                    if (data1.Length >= 1)



                        if (data1.Contains("%B") || data1.Contains("%R"))
                        {
                            m_cardnumber = data1.Replace("%B", "").Replace("%R", "").Replace("?", "").Replace("\r", "");
                            tbGiftCard.Text = m_cardnumber;

                           // this.tbTemp.Text = "";
                            balance = paymentviewmodel.CheckBalance(m_cardnumber);

                            //verify if card has been activated (0 or greater)
                            if (balance == -99)
                            {
                                tbMessage.Foreground = Brushes.Red;
                                tbMessage.Text = "Gift Card NOT Activated";
                               // RawString = "";
                            }
                            //verify if card has already been used on this ticket
                            if (balance == -100)
                            {
                                tbMessage.Foreground = Brushes.Red;
                                tbMessage.Text = "Gift Card already used on this ticket";
                                // RawString = "";
                            }
                                trackcount = 0;
                        }
                }
            }
            catch (Exception e)
            {
                tbMessage.Foreground = Brushes.Red;
                tbMessage.Text = "Error reading card .. please try again" + e.Message;
               RawString = "";
                trackcount = 0;
            }
        }

    }
}
